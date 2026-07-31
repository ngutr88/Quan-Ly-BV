using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Models.ViewModels;
using QuanLyBenhVien.Models.ViewModels.PrescriptionSafety;
using QuanLyBenhVien.Models.ViewModels.LabDiagnostics;
using QuanLyBenhVien.Services;
using System.Text.Json;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class ExamController : Controller
    {
        private const int PediatricAgeThreshold = 15;

        private readonly ApplicationDbContext _context;
        private readonly DoctorDashboardNotifier _notifier;
        private readonly IPrescriptionSafetyChecker _safetyChecker;
        private readonly MedicineStockAllocator _stockAllocator;

        public ExamController(
            ApplicationDbContext context,
            DoctorDashboardNotifier notifier,
            IPrescriptionSafetyChecker safetyChecker,
            MedicineStockAllocator stockAllocator)
        {
            _context = context;
            _notifier = notifier;
            _safetyChecker = safetyChecker;
            _stockAllocator = stockAllocator;
        }

        // GET: /Doctor/Exam/Session/5
        [HttpGet]
        public async Task<IActionResult> Session(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var appointment = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.BacSiId == doctorId.Value);

            if (appointment == null) return NotFound();

            // Set state to "DangKham" (In-progress examination)
            if (appointment.TrangThai == "DaXacNhan")
            {
                appointment.TrangThai = "DangKham";
                _context.Entry(appointment).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await _notifier.NotifyQueueUpdatedAsync(doctorId);
            }

            // Fetch patient medical history (past exams)
            ViewBag.HistoryExams = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == appointment.BenhNhanId && e.LichKhamId != id)
                .OrderByDescending(e => e.NgayKham)
                .Take(5)
                .ToListAsync();
            ViewBag.PatientDocuments = await _context.PatientDocuments
                .Where(d => d.BenhNhanId == appointment.BenhNhanId)
                .OrderByDescending(d => d.NgayTaiLen)
                .ToListAsync();

            // Khung ngữ cảnh bệnh nhân khi kê đơn (A1): thuốc đang dùng từ đơn
            // còn hiệu lực + chỉ số sinh hiệu gần nhất. KHÔNG có chỉ số chức
            // năng gan/thận (creatinine/eGFR/AST/ALT) ở bất kỳ đâu trong hệ
            // thống - view phải hiện rõ "chưa có dữ liệu" thay vì bịa.
            ViewBag.ActivePrescriptionDrugs = await _context.PrescriptionDetails
                .Where(pd => (pd.Prescription.TrangThai == "ChoCapPhat" || pd.Prescription.TrangThai == "DaCapPhat")
                    && pd.Prescription.ExaminationRecord.Appointment.BenhNhanId == appointment.BenhNhanId)
                .Select(pd => pd.Medicine.TenThuoc)
                .Distinct()
                .ToListAsync();
            ViewBag.RecentHealthMetric = await _context.PatientHealthMetrics
                .Where(m => m.BenhNhanId == appointment.BenhNhanId)
                .OrderByDescending(m => m.NgayDo)
                .FirstOrDefaultAsync();
            ViewBag.PatientAgeYears = (int)((DateTime.Today - appointment.Patient.NgaySinh).TotalDays / 365.25);

            // Chỉ định CLS (Giai đoạn 1): danh mục dịch vụ đang hoạt động, nhóm
            // theo NhomCLS để JS dựng danh sách chọn theo nhóm + các bộ chỉ
            // định có sẵn để "áp dụng nhanh" một lần.
            ViewBag.CLSCatalog = await _context.LabServiceCatalogs
                .Where(s => s.DangHoatDong)
                .OrderBy(s => s.NhomCLS).ThenBy(s => s.TenDichVu)
                .Select(s => new LabServiceOptionViewModel { Id = s.Id, NhomCLS = s.NhomCLS, TenDichVu = s.TenDichVu, Gia = s.Gia })
                .ToListAsync();
            ViewBag.CLSBundles = await _context.LabOrderBundles
                .Where(b => b.DangHoatDong)
                .Include(b => b.ThanhVien)
                .Select(b => new LabBundleOptionViewModel { Id = b.Id, TenBo = b.TenBo, DichVuIds = b.ThanhVien.Select(i => i.DichVuCLSId).ToList() })
                .ToListAsync();

            return View(appointment);
        }

        // GET: /Doctor/Exam/SearchMedicines?q=para&page=1
        [HttpGet]
        public async Task<IActionResult> SearchMedicines(string q, int page = 1)
        {
            const int pageSize = 20;
            var query = _context.Medicines.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(m => m.TenThuoc.ToLower().Contains(term) || m.HoatChat.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var pageItems = await query
                .OrderBy(m => m.TenThuoc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<object>();
            foreach (var m in pageItems)
            {
                var outOfStock = m.TonKho <= 0;
                List<object>? alternatives = null;
                if (outOfStock)
                {
                    alternatives = await _context.Medicines.AsNoTracking()
                        .Where(alt => alt.Id != m.Id && alt.HoatChat == m.HoatChat && alt.TonKho > 0)
                        .OrderByDescending(alt => alt.TonKho)
                        .Take(3)
                        .Select(alt => new { id = alt.Id, name = alt.TenThuoc, stock = alt.TonKho })
                        .ToListAsync<object>();
                }

                items.Add(new
                {
                    id = m.Id,
                    name = m.TenThuoc,
                    ingredient = m.HoatChat,
                    strength = m.HamLuong,
                    packSpec = m.QuyCachDongGoi,
                    unit = m.DonViTinh,
                    price = m.Gia,
                    stock = m.TonKho,
                    outOfStock,
                    coBaoHiemYTe = m.CoBaoHiemYTe,
                    alternatives
                });
            }

            return Json(new { success = true, items, hasMore = page * pageSize < totalCount });
        }

        // POST: /Doctor/Exam/CheckSafety - chạy lại mỗi khi thêm/sửa/xóa thuốc
        // trong đơn đang soạn, kiểm tra TOÀN BỘ danh sách hiện tại (không chỉ
        // dòng vừa thêm), vì tương tác/trùng hoạt chất có thể liên quan đến
        // bất kỳ cặp thuốc nào trong đơn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckSafety(int patientId, decimal? canNangKg, string linesJson)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var assignedPatient = await _context.Appointments.AnyAsync(a =>
                a.BacSiId == doctorId.Value && a.BenhNhanId == patientId && a.TrangThai != "DaHuy");
            if (!assignedPatient) return Forbid();

            List<CandidateDrugLine> candidateLines;
            try
            {
                candidateLines = string.IsNullOrWhiteSpace(linesJson) || linesJson == "[]"
                    ? new List<CandidateDrugLine>()
                    : JsonSerializer.Deserialize<List<CandidateDrugLine>>(linesJson) ?? new List<CandidateDrugLine>();
            }
            catch (JsonException)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đơn thuốc không đúng định dạng." });
            }

            var result = await _safetyChecker.CheckAsync(new PrescriptionSafetyContext
            {
                PatientId = patientId,
                CanNangKg = canNangKg,
                CandidateLines = candidateLines
            });

            return Json(new
            {
                success = true,
                warnings = result.Warnings.Select(w => new
                {
                    tier = w.Tier.ToString(),
                    category = w.Category,
                    message = w.Message,
                    requiresAcknowledgement = w.RequiresAcknowledgement,
                    relatedMedicineIdA = w.RelatedMedicineIdA,
                    relatedMedicineIdB = w.RelatedMedicineIdB
                })
            });
        }

        // POST: /Doctor/Exam/CompleteSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSession(int appointmentId, string trieuChung, string huyetAp, int? nhipTim, decimal? nhietDo, decimal? canNang, decimal? chieuCao, string chanDoan, string loiDan, string chiDinhCls, string ketQuaCls, DateTime? ngayHenTaiKham, string presJson, string? clsOrderJson)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var appointment = await _context.Appointments
                .Include(a => a.Patient.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BacSiId == doctorId.Value);

            if (appointment == null) return NotFound();
            if (appointment.TrangThai == "DaHuy" || appointment.TrangThai == "VangMat") return BadRequest("Lịch khám không còn hiệu lực để hoàn tất.");
            if (await _context.ExaminationRecords.AnyAsync(e => e.LichKhamId == appointmentId))
                return BadRequest("Phiên khám này đã được hoàn tất trước đó.");

            PrescriptionSubmissionPayload payload;
            try
            {
                payload = string.IsNullOrWhiteSpace(presJson) || presJson == "[]"
                    ? new PrescriptionSubmissionPayload()
                    : JsonSerializer.Deserialize<PrescriptionSubmissionPayload>(presJson) ?? new PrescriptionSubmissionPayload();
            }
            catch (JsonException)
            {
                return BadRequest("Đơn thuốc không đúng định dạng.");
            }

            LabOrderSubmissionPayload clsPayload;
            try
            {
                clsPayload = string.IsNullOrWhiteSpace(clsOrderJson) || clsOrderJson == "[]"
                    ? new LabOrderSubmissionPayload()
                    : JsonSerializer.Deserialize<LabOrderSubmissionPayload>(clsOrderJson) ?? new LabOrderSubmissionPayload();
            }
            catch (JsonException)
            {
                return BadRequest("Chỉ định cận lâm sàng không đúng định dạng.");
            }

            var selectedClsServices = clsPayload.DichVuCLSIds.Count > 0
                ? await _context.LabServiceCatalogs
                    .Where(s => clsPayload.DichVuCLSIds.Contains(s.Id) && s.DangHoatDong)
                    .ToListAsync()
                : new List<LabServiceCatalog>();

            if (selectedClsServices.Count != clsPayload.DichVuCLSIds.Distinct().Count())
                return BadRequest("Một hoặc nhiều dịch vụ cận lâm sàng không hợp lệ.");

            foreach (var line in payload.Lines)
            {
                if (line.MedicineId <= 0 || line.LieuMoiLan <= 0 || line.SoLanMoiNgay <= 0 || line.SoNgayDung <= 0 || string.IsNullOrWhiteSpace(line.DuongDung))
                    return BadRequest("Thông tin liều dùng của một hoặc nhiều thuốc chưa hợp lệ.");
            }

            // Bắt buộc cân nặng cho bệnh nhi <15 tuổi trước khi kê đơn (thực thi
            // ở server, không chỉ gợi ý phía client).
            var ageYears = (DateTime.Today - appointment.Patient.NgaySinh).TotalDays / 365.25;
            if (payload.Lines.Count > 0 && ageYears < PediatricAgeThreshold && !canNang.HasValue)
                return BadRequest("Bắt buộc nhập cân nặng cho bệnh nhân dưới 15 tuổi trước khi kê đơn.");

            var medicines = await _context.Medicines
                .Include(m => m.LoThuocs)
                .Where(m => payload.Lines.Select(l => l.MedicineId).Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            if (payload.Lines.Any(l => !medicines.ContainsKey(l.MedicineId)))
                return BadRequest("Một hoặc nhiều thuốc không tồn tại.");

            // Tính số lượng ở SERVER (không tin số lượng client gửi), làm tròn
            // lên bội số đóng gói nếu có khai báo.
            var computedQuantities = new Dictionary<int, int>();
            foreach (var line in payload.Lines)
            {
                var medicine = medicines[line.MedicineId];
                var rawQty = (int)Math.Ceiling(line.LieuMoiLan * line.SoLanMoiNgay * line.SoNgayDung);
                if (medicine.QuyCachSoLuong.HasValue && medicine.QuyCachSoLuong.Value > 0)
                {
                    var pack = medicine.QuyCachSoLuong.Value;
                    rawQty = ((rawQty + pack - 1) / pack) * pack;
                }
                computedQuantities[line.MedicineId] = computedQuantities.TryGetValue(line.MedicineId, out var existing) ? existing + rawQty : rawQty;
            }

            foreach (var kv in computedQuantities)
            {
                var medicine = medicines[kv.Key];
                var available = medicine.LoThuocs
                    .Where(b => b.HanSuDung > DateTime.Today && b.SoLuongTon > 0)
                    .Sum(b => b.SoLuongTon);
                if (available < kv.Value || medicine.TonKho < kv.Value)
                    return BadRequest($"Thuốc {medicine.TenThuoc} không đủ tồn kho hợp lệ.");
            }

            // Chạy lại TOÀN BỘ kiểm tra an toàn ở server khi lưu - không chỉ tin
            // kết quả xem trước phía client (đóng lỗ hổng "chỉ kiểm tra tham khảo").
            if (payload.Lines.Count > 0)
            {
                var safetyResult = await _safetyChecker.CheckAsync(new PrescriptionSafetyContext
                {
                    PatientId = appointment.BenhNhanId,
                    CanNangKg = canNang,
                    CandidateLines = payload.Lines.Select(l => new CandidateDrugLine
                    {
                        MedicineId = l.MedicineId,
                        LieuMoiLan = l.LieuMoiLan,
                        SoLanMoiNgay = l.SoLanMoiNgay,
                        SoNgayDung = l.SoNgayDung
                    }).ToList()
                });

                var unacknowledged = safetyResult.Warnings
                    .Where(w => w.RequiresAcknowledgement && !payload.AcknowledgedCategories.Contains(w.Category))
                    .ToList();
                if (unacknowledged.Count > 0)
                    return BadRequest($"Cần xác nhận cảnh báo trước khi lưu: {unacknowledged[0].Message}");

                var anyOverridden = safetyResult.Warnings.Any(w => w.RequiresAcknowledgement);
                if (anyOverridden && string.IsNullOrWhiteSpace(payload.OverrideReason))
                    return BadRequest("Vui lòng nhập lý do khi ghi đè cảnh báo an toàn.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Nối tên các dịch vụ CLS đã chọn vào chuỗi ChiDinhCLS tự do hiện có
            // - giữ nguyên đường xem cũ (bệnh án, Cổng Bệnh nhân) vẫn hiển thị
            // được nội dung có ý nghĩa mà không cần sửa các màn đó.
            var combinedChiDinhCls = chiDinhCls ?? string.Empty;
            if (selectedClsServices.Count > 0)
            {
                var clsNames = string.Join(", ", selectedClsServices.Select(s => s.TenDichVu));
                combinedChiDinhCls = string.IsNullOrWhiteSpace(combinedChiDinhCls)
                    ? clsNames
                    : $"{combinedChiDinhCls}; {clsNames}";
            }

            // 1. Create Examination Record
            var exam = new ExaminationRecord
            {
                LichKhamId = appointmentId,
                TrieuChung = trieuChung,
                HuyetAp = huyetAp ?? string.Empty,
                NhipTim = nhipTim,
                NhietDo = nhietDo,
                CanNang = canNang,
                ChieuCao = chieuCao,
                ChanDoan = chanDoan,
                LoiDan = loiDan ?? string.Empty,
                ChiDinhCLS = combinedChiDinhCls,
                KetQuaCLS = ketQuaCls ?? string.Empty,
                NgayHenTaiKham = ngayHenTaiKham,
                NgayKham = DateTime.Now
            };

            if (canNang.HasValue && chieuCao.HasValue && chieuCao.Value > 0)
            {
                decimal heightInMeters = chieuCao.Value / 100;
                exam.BMI = canNang.Value / (heightInMeters * heightInMeters);
            }

            _context.ExaminationRecords.Add(exam);
            await _context.SaveChangesAsync(); // Generates exam.Id

            if (selectedClsServices.Count > 0)
            {
                var labOrder = new LabOrder
                {
                    MaPhieu = await GenerateNextLabOrderMaPhieuAsync(DateTime.Now.Year),
                    PhieuKhamId = exam.Id,
                    BenhNhanId = appointment.BenhNhanId,
                    BacSiChiDinhId = doctorId.Value,
                    BoChiDinhCLSId = clsPayload.BoChiDinhCLSId,
                    GhiChuChiDinh = string.IsNullOrWhiteSpace(clsPayload.GhiChu) ? null : clsPayload.GhiChu!.Trim(),
                    NgayChiDinh = DateTime.Now
                };
                _context.LabOrders.Add(labOrder);
                await _context.SaveChangesAsync(); // Generates labOrder.Id

                foreach (var service in selectedClsServices)
                {
                    _context.LabOrderItems.Add(new LabOrderItem
                    {
                        PhieuChiDinhCLSId = labOrder.Id,
                        DichVuCLSId = service.Id,
                        TrangThai = "ChoThucHien"
                    });
                }
                await _context.SaveChangesAsync();
            }

            decimal drugCost = 0;
            Prescription? prescription = null;

            if (payload.Lines.Count > 0)
            {
                prescription = new Prescription
                {
                    PhieuKhamId = exam.Id,
                    BacSiKeId = doctorId.Value,
                    TrangThai = "ChoCapPhat",
                    NgayKe = DateTime.Now
                };
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync(); // Generates prescription.Id

                foreach (var line in payload.Lines)
                {
                    var medicine = medicines[line.MedicineId];
                    var qty = computedQuantities[line.MedicineId];
                    var huongDan = !string.IsNullOrWhiteSpace(line.HuongDanSuDungOverride)
                        ? line.HuongDanSuDungOverride!.Trim()
                        : ComposeInstructions(line, medicine.DonViTinh);

                    var detail = new PrescriptionDetail
                    {
                        DonThuocId = prescription.Id,
                        ThuocId = line.MedicineId,
                        LieuDung = huongDan,
                        SoLuong = qty,
                        LieuMoiLan = line.LieuMoiLan,
                        SoLanMoiNgay = line.SoLanMoiNgay,
                        DuongDung = line.DuongDung,
                        ThoiDiemDung = line.ThoiDiemDung,
                        SoNgayDung = line.SoNgayDung,
                        HuongDanSuDung = huongDan
                    };
                    _context.PrescriptionDetails.Add(detail);
                    drugCost += qty * medicine.Gia;
                }
                await _context.SaveChangesAsync(); // Generates PrescriptionDetail.Id cho từng dòng

                foreach (var line in payload.Lines)
                {
                    var qty = computedQuantities[line.MedicineId];
                    var detail = await _context.PrescriptionDetails
                        .Where(d => d.DonThuocId == prescription.Id && d.ThuocId == line.MedicineId)
                        .OrderByDescending(d => d.Id)
                        .FirstAsync();
                    // Đã trừ cho hoạt chất này ở lượt trước (2 dòng cùng thuốc đã gộp
                    // số lượng ở computedQuantities) - chỉ phân bổ lô 1 lần/thuốc.
                    var alreadyAllocated = await _context.PrescriptionBatchAllocations
                        .AnyAsync(a => a.ChiTietDonThuoc.DonThuocId == prescription.Id && a.ChiTietDonThuoc.ThuocId == line.MedicineId);
                    if (alreadyAllocated) continue;

                    var allocated = await _stockAllocator.AllocateFefoAsync(line.MedicineId, detail.Id, qty);
                    if (!allocated)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Thuốc {medicines[line.MedicineId].TenThuoc} không đủ tồn kho hợp lệ.");
                    }
                }
                await _context.SaveChangesAsync();

                // Ghi audit log cho mọi cảnh báo bị ghi đè (không chứa CCCD/BHYT/
                // số liệu sức khỏe, chỉ tên thuốc + loại cảnh báo + lý do bác sĩ).
                var safetyResultForAudit = await _safetyChecker.CheckAsync(new PrescriptionSafetyContext
                {
                    PatientId = appointment.BenhNhanId,
                    CanNangKg = canNang,
                    CandidateLines = payload.Lines.Select(l => new CandidateDrugLine
                    {
                        MedicineId = l.MedicineId,
                        LieuMoiLan = l.LieuMoiLan,
                        SoLanMoiNgay = l.SoLanMoiNgay,
                        SoNgayDung = l.SoNgayDung
                    }).ToList()
                });
                foreach (var warning in safetyResultForAudit.Warnings.Where(w => w.RequiresAcknowledgement))
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = GetCurrentUserId(),
                        HanhDong = $"Ghi đè cảnh báo kê đơn ({warning.Category})",
                        ChiTiet = $"{warning.Message} Lý do bác sĩ: {payload.OverrideReason}",
                        DoiTuongLoai = "DonThuoc",
                        DoiTuongId = prescription.Id
                    });
                }
            }

            // 3. Update Appointment Status
            appointment.TrangThai = "HoanThanh";
            _context.Entry(appointment).State = EntityState.Modified;

            // 4. Auto-generate Invoice (HoaDon)
            var invoice = new Invoice
            {
                PhieuKhamId = exam.Id,
                TongTien = 150000 + drugCost, // Consult fee (150K) + drug charges
                TrangThaiThanhToan = "ChuaThanhToan",
                PhuongThuc = "TienMat",
                NgayTao = DateTime.Now
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _context.InvoiceDetails.Add(new InvoiceDetail
            {
                HoaDonId = invoice.Id,
                LoaiPhi = "Phí khám lâm sàng",
                SoTien = 150000
            });

            if (drugCost > 0)
            {
                _context.InvoiceDetails.Add(new InvoiceDetail
                {
                    HoaDonId = invoice.Id,
                    LoaiPhi = "Thuốc theo đơn",
                    SoTien = drugCost
                });
            }

            // 5. System Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Hoàn tất khám bệnh",
                ChiTiet = $"Bác sĩ hoàn tất khám và kê đơn cho BN {appointment.Patient.User.HoTen} (LK #{appointmentId}), chẩn đoán: {chanDoan}. Viện phí: {invoice.TongTien:N0}đ."
            });

            // 6. Create Notifications for Patient
            if (drugCost > 0)
            {
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = appointment.Patient.NguoiDungId,
                    NoiDung = "[DonThuoc] Đơn thuốc mới|Bác sĩ vừa cập nhật đơn thuốc mới cho hồ sơ bệnh án của bạn.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });
            }

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = appointment.Patient.NguoiDungId,
                NoiDung = $"[ThanhToan] Yêu cầu thanh toán|Hóa đơn viện phí mới #INV-{invoice.Id:D4} trị giá {invoice.TongTien:N0}đ đã được khởi tạo. Vui lòng tiến hành thanh toán.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _notifier.NotifyQueueUpdatedAsync(doctorId);
            await _notifier.NotifyActionRequiredUpdatedAsync(doctorId);

            TempData["SuccessMessage"] = $"Đã hoàn tất ca khám cho bệnh nhân {appointment.Patient.User.HoTen}. Đơn thuốc và hóa đơn đã được khởi tạo thành công.";
            return RedirectToAction("Index", "Queue");
        }

        private async Task<string> GenerateNextLabOrderMaPhieuAsync(int year)
        {
            var prefix = $"CLS-{year}-";
            var maxSeq = (await _context.LabOrders
                .Where(o => o.MaPhieu.StartsWith(prefix))
                .Select(o => o.MaPhieu)
                .ToListAsync())
                .Select(m => int.TryParse(m.Substring(prefix.Length), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            return $"{prefix}{maxSeq + 1:D4}";
        }

        private static string ComposeInstructions(PrescriptionLineSubmission line, string donViTinh)
        {
            var thoiDiem = string.IsNullOrWhiteSpace(line.ThoiDiemDung) ? string.Empty : $", {line.ThoiDiemDung}";
            return $"{line.DuongDung} {line.LieuMoiLan} {donViTinh}/lần, {line.SoLanMoiNgay} lần/ngày{thoiDiem}, dùng trong {line.SoNgayDung} ngày";
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return null;
            return await _context.Doctors
                .Where(d => d.NguoiDungId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
