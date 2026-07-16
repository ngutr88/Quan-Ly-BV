using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using System.Text.Json;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class ExamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamController(ApplicationDbContext context)
        {
            _context = context;
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
            }

            // Fetch patient medical history (past exams)
            ViewBag.HistoryExams = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == appointment.BenhNhanId && e.LichKhamId != id)
                .OrderByDescending(e => e.NgayKham)
                .Take(5)
                .ToListAsync();

            // Fetch drug catalog for search
            ViewBag.Medicines = await _context.Medicines.Where(m => m.TonKho > 0).ToListAsync();

            return View(appointment);
        }

        // API Endpoint: /Doctor/Exam/CheckAllergiesAndStock
        [HttpPost]
        public async Task<IActionResult> CheckAllergiesAndStock(int patientId, int medicineId, int qty)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue || qty <= 0) return Forbid();

            var assignedPatient = await _context.Appointments.AnyAsync(a =>
                a.BacSiId == doctorId.Value &&
                a.BenhNhanId == patientId &&
                a.TrangThai != "DaHuy");
            if (!assignedPatient) return Forbid();

            var patient = await _context.Patients.FindAsync(patientId);
            var medicine = await _context.Medicines.FindAsync(medicineId);

            if (patient == null || medicine == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu Bệnh nhân hoặc Thuốc." });
            }

            // 1. Check Inventory
            if (medicine.TonKho < qty)
            {
                return Json(new { success = true, hasWarning = true, warningType = "stock", message = $"Kho không đủ thuốc. Hiện tại chỉ còn {medicine.TonKho} {medicine.DonViTinh}." });
            }

            // 2. Check Allergies
            bool hasAllergy = false;
            if (!string.IsNullOrEmpty(patient.DiUng))
            {
                var allergies = patient.DiUng.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var allergy in allergies)
                {
                    if (medicine.TenThuoc.Contains(allergy, StringComparison.OrdinalIgnoreCase) || 
                        medicine.HoatChat.Contains(allergy, StringComparison.OrdinalIgnoreCase))
                    {
                        hasAllergy = true;
                        break;
                    }
                }
            }

            if (hasAllergy)
            {
                return Json(new { 
                    success = true, 
                    hasWarning = true, 
                    warningType = "allergy", 
                    message = $"Cảnh báo nguy hiểm: Bệnh nhân có tiền sử dị ứng với nhóm thuốc chứa hoạt chất '{medicine.HoatChat}' (Khai báo dị ứng: {patient.DiUng})." 
                });
            }

            return Json(new { success = true, hasWarning = false });
        }

        // POST: /Doctor/Exam/CompleteSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSession(int appointmentId, string trieuChung, string huyetAp, int? nhipTim, decimal? nhietDo, decimal? canNang, decimal? chieuCao, string chanDoan, string loiDan, string chiDinhCls, string ketQuaCls, string presJson)
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
                ChiDinhCLS = chiDinhCls ?? string.Empty,
                KetQuaCLS = ketQuaCls ?? string.Empty,
                NgayKham = DateTime.Now
            };

            // Calculate BMI
            if (canNang.HasValue && chieuCao.HasValue && chieuCao.Value > 0)
            {
                // height in cm -> convert to meters
                decimal heightInMeters = chieuCao.Value / 100;
                exam.BMI = canNang.Value / (heightInMeters * heightInMeters);
            }

            _context.ExaminationRecords.Add(exam);
            await _context.SaveChangesAsync(); // Generates exam.Id

            decimal drugCost = 0;
            var prescriptionsList = new List<PrescriptionDetail>();

            // 2. Parse and Create Prescription Details (if any drugs prescribed)
            if (!string.IsNullOrEmpty(presJson) && presJson != "[]")
            {
                var prescription = new Prescription
                {
                    PhieuKhamId = exam.Id,
                    NgayKe = DateTime.Now
                };
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                var items = JsonSerializer.Deserialize<List<TempPrescriptionItem>>(presJson);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                        if (medicine == null) continue;

                        // Create Prescription Detail link
                        var detail = new PrescriptionDetail
                        {
                            DonThuocId = prescription.Id,
                            ThuocId = item.MedicineId,
                            LieuDung = item.Instructions,
                            SoLuong = item.Qty
                        };
                        _context.PrescriptionDetails.Add(detail);

                        // Deduct Stock following FEFO algorithm (First Expired, First Out)
                        int remainingToDeduct = item.Qty;
                        
                        var activeBatches = await _context.MedicineBatches
                            .Where(b => b.ThuocId == item.MedicineId && b.SoLuongTon > 0)
                            .OrderBy(b => b.HanSuDung) // Oldest batches (soonest expiry) first
                            .ToListAsync();

                        foreach (var batch in activeBatches)
                        {
                            if (remainingToDeduct <= 0) break;

                            if (batch.SoLuongTon >= remainingToDeduct)
                            {
                                batch.SoLuongTon -= remainingToDeduct;
                                remainingToDeduct = 0;
                            }
                            else
                            {
                                remainingToDeduct -= batch.SoLuongTon;
                                batch.SoLuongTon = 0;
                            }
                            _context.Entry(batch).State = EntityState.Modified;
                        }

                        // Update overall medicine inventory
                        medicine.TonKho = Math.Max(0, medicine.TonKho - item.Qty);
                        _context.Entry(medicine).State = EntityState.Modified;

                        // Calculate cost
                        drugCost += (item.Qty * medicine.Gia);
                    }
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

            // Add Invoice Details
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

            TempData["SuccessMessage"] = $"Đã hoàn tất ca khám cho bệnh nhân {appointment.Patient.User.HoTen}. Đơn thuốc và hóa đơn đã được khởi tạo thành công.";
            return RedirectToAction("Index", "Queue");
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

        private class TempPrescriptionItem
        {
            public int MedicineId { get; set; }
            public int Qty { get; set; }
            public string Instructions { get; set; } = string.Empty;
        }
    }
}
