using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelExportService _excel;
        private readonly IWebHostEnvironment _environment;

        public PatientsController(ApplicationDbContext context, ExcelExportService excel, IWebHostEnvironment environment)
        {
            _context = context;
            _excel = excel;
            _environment = environment;
        }

        // GET: Admin/Patients
        public async Task<IActionResult> Index(string searchString, string status, int page = 1, int? pageSize = null)
        {
            var query = BuildQuery(searchString, status);

            var paged = await query.ToPagedListAsync(page, PagedList<QuanLyBenhVien.Models.Patient>.NormalisePageSize(pageSize));

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = status;

            // Cohort tiles describe the whole filtered set, not just this page.
            ViewBag.TotalMatching = paged.TotalCount;
            ViewBag.ActiveCount = await query.CountAsync(p => p.User.TrangThai == "Active");
            ViewBag.BlockedCount = await query.CountAsync(p => p.User.TrangThai != "Active");
            ViewBag.InsuredCount = await query.CountAsync(p => p.SoBHYT != null && p.SoBHYT != "");

            return View(paged);
        }

        // GET: Admin/Patients/Export
        public async Task<IActionResult> Export(string searchString, string status)
        {
            var patients = await BuildQuery(searchString, status).ToListAsync();

            var columns = new List<ExcelColumn<QuanLyBenhVien.Models.Patient>>
            {
                new("Mã BN", p => $"BN-{p.Id:D4}"),
                new("Họ và tên", p => p.User.HoTen),
                new("Ngày sinh", p => p.NgaySinh.ToString("dd/MM/yyyy")),
                new("Giới tính", p => p.GioiTinh),
                new("Nhóm máu", p => p.NhomMau),
                new("Số CCCD", p => p.SoCCCD),
                new("Số BHYT", p => p.SoBHYT),
                new("Số điện thoại", p => p.User.Sdt),
                new("Email", p => p.User.Email),
                new("Trạng thái", p => p.User.TrangThai == "Active" ? "Hoạt động" : "Bị khóa"),
                new("Ngày tạo hồ sơ", p => p.User.NgayTao.ToString("dd/MM/yyyy"))
            };

            var content = _excel.Build(
                "Benh nhan",
                "DANH SÁCH BỆNH NHÂN",
                columns,
                patients,
                BuildFilterSummary(searchString, status));

            return File(content, ExcelExportService.ContentType, ExcelExportService.FileName("danh-sach-benh-nhan"));
        }

        private IQueryable<QuanLyBenhVien.Models.Patient> BuildQuery(string searchString, string status)
        {
            var query = _context.Patients
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.User.HoTen.Contains(searchString) ||
                                         p.User.Sdt.Contains(searchString) ||
                                         p.SoBHYT.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.User.TrangThai == status);
            }

            return query.OrderBy(p => p.User.HoTen);
        }

        private static string BuildFilterSummary(string searchString, string status)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(searchString))
            {
                parts.Add($"Từ khóa: {searchString}");
            }
            if (!string.IsNullOrEmpty(status))
            {
                parts.Add($"Trạng thái: {(status == "Active" ? "Hoạt động" : "Bị khóa")}");
            }
            return parts.Count == 0 ? "Toàn bộ hồ sơ" : string.Join(" • ", parts);
        }

        // GET: Admin/Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Dependents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            // Historical Appointments of this patient
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id)
                .OrderByDescending(a => a.ThoiGian)
                .ToListAsync();

            // Historical Examination Records (Medical History)
            ViewBag.ExaminationRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            // Prescriptions issued per examination (keyed by PhieuKhamId in the view)
            ViewBag.Prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                    .ThenInclude(pd => pd.Medicine)
                .Where(p => p.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .ToListAsync();

            // Uploaded documents (results, referral letters, etc.)
            ViewBag.Documents = await _context.PatientDocuments
                .Where(d => d.BenhNhanId == patient.Id)
                .OrderByDescending(d => d.NgayTaiLen)
                .ToListAsync();

            // Historical Invoices & payment status
            ViewBag.Invoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            // Family history & immunization records
            ViewBag.FamilyHistories = await _context.FamilyHistories
                .Where(f => f.BenhNhanId == patient.Id)
                .OrderByDescending(f => f.NgayGhiNhan)
                .ToListAsync();

            ViewBag.Immunizations = await _context.Immunizations
                .Where(im => im.BenhNhanId == patient.Id)
                .OrderByDescending(im => im.NgayTiem)
                .ToListAsync();

            // Most recent audit entry targeting this patient, for a "last updated by" line.
            ViewBag.LastAudit = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.DoiTuongLoai == "BenhNhan" && a.DoiTuongId == patient.Id)
                .OrderByDescending(a => a.ThoiGian)
                .FirstOrDefaultAsync();

            // Department/doctor lookups for the "create appointment" form
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync();
            ViewBag.AllDoctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.User.TrangThai == "Active")
                .OrderBy(d => d.User.HoTen)
                .ToListAsync();

            return View(patient);
        }

        // GET: Admin/Patients/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id);
            if (document == null) return NotFound();

            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, document.ContentType, document.TenTaiLieu);
        }

        // POST: Admin/Patients/UpdateAllergy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllergy(int id, string diUng)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            patient.DiUng = string.IsNullOrWhiteSpace(diUng) ? string.Empty : diUng.Trim();
            _context.Entry(patient).State = EntityState.Modified;

            LogPatientAudit(patient.Id, "Cập nhật tiền sử dị ứng", $"Cập nhật tiền sử dị ứng của bệnh nhân BN-{patient.Id:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật tiền sử dị ứng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Patients/RevealSensitive/5?field=cccd
        [HttpGet]
        public async Task<IActionResult> RevealSensitive(int id, string field)
        {
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return NotFound();

            string value;
            string fieldLabel;
            switch (field)
            {
                case "cccd":
                    value = patient.SoCCCD;
                    fieldLabel = "số CCCD";
                    break;
                case "phone":
                    value = patient.User.Sdt;
                    fieldLabel = "số điện thoại";
                    break;
                default:
                    return BadRequest();
            }

            LogPatientAudit(patient.Id, "Xem thông tin nhạy cảm", $"Xem đầy đủ {fieldLabel} của bệnh nhân BN-{patient.Id:D4}.");
            await _context.SaveChangesAsync();

            return Json(new { value });
        }

        // GET: Admin/Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Admin/Patients/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string hoTen, string sdt, DateTime ngaySinh, string gioiTinh, string nhomMau, string soBHYT, DateTime? ngayHetHanBHYT, string soCCCD, string tienSuBenh)
        {
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return NotFound();

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(sdt))
            {
                TempData["ErrorMessage"] = "Họ tên và số điện thoại là bắt buộc.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            patient.User.HoTen = hoTen.Trim();
            patient.User.Sdt = sdt.Trim();
            patient.NgaySinh = ngaySinh;
            patient.GioiTinh = gioiTinh ?? string.Empty;
            patient.NhomMau = string.IsNullOrWhiteSpace(nhomMau) ? patient.NhomMau : nhomMau;
            patient.SoBHYT = soBHYT?.Trim() ?? string.Empty;
            patient.NgayHetHanBHYT = ngayHetHanBHYT;
            patient.SoCCCD = soCCCD?.Trim() ?? string.Empty;
            patient.TienSuBenh = tienSuBenh?.Trim() ?? string.Empty;

            _context.Entry(patient).State = EntityState.Modified;
            _context.Entry(patient.User).State = EntityState.Modified;

            LogPatientAudit(id, "Cập nhật thông tin bệnh nhân", $"Cập nhật thông tin hành chính của bệnh nhân BN-{id:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật thông tin bệnh nhân.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Patients/AddDependent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependent(int patientId, string hoTen, string quanHe, string gioiTinh, int namSinh, string nhomMau, string soBHYT, string tienSuBenhLy)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            _context.Dependents.Add(new Dependent
            {
                BenhNhanId = patientId,
                HoTen = hoTen?.Trim() ?? string.Empty,
                QuanHe = quanHe?.Trim() ?? string.Empty,
                GioiTinh = gioiTinh ?? string.Empty,
                NamSinh = namSinh,
                NhomMau = string.IsNullOrWhiteSpace(nhomMau) ? "O+" : nhomMau,
                SoBHYT = soBHYT?.Trim() ?? string.Empty,
                TienSuBenhLy = tienSuBenhLy?.Trim() ?? string.Empty
            });

            LogPatientAudit(patientId, "Thêm người thân", $"Thêm người thân '{hoTen}' cho bệnh nhân BN-{patientId:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm người thân.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // POST: Admin/Patients/UploadDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int patientId, IFormFile file, string tenTaiLieu, string ghiChu)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp để tải lên.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension) || file.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ PDF/JPG/PNG và dung lượng tối đa 10MB.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var storageRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents");
            Directory.CreateDirectory(storageRoot);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var storedPath = Path.Combine(storageRoot, storedName);
            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            _context.PatientDocuments.Add(new PatientDocument
            {
                BenhNhanId = patientId,
                TenTaiLieu = string.IsNullOrWhiteSpace(tenTaiLieu) ? Path.GetFileName(file.FileName) : tenTaiLieu.Trim(),
                LoaiTaiLieu = "GiayToKhamBenh",
                TenLuuTru = storedName,
                ContentType = file.ContentType ?? "application/octet-stream",
                KichThuoc = file.Length,
                GhiChu = ghiChu?.Trim() ?? string.Empty
            });

            LogPatientAudit(patientId, "Tải tài liệu", $"Tải lên tài liệu cho bệnh nhân BN-{patientId:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tải lên tài liệu.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // POST: Admin/Patients/AddFamilyHistory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFamilyHistory(int patientId, string quanHe, string tenBenh, string ghiChu)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (string.IsNullOrWhiteSpace(quanHe) || string.IsNullOrWhiteSpace(tenBenh))
            {
                TempData["ErrorMessage"] = "Quan hệ và tên bệnh là bắt buộc.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            _context.FamilyHistories.Add(new FamilyHistory
            {
                BenhNhanId = patientId,
                QuanHe = quanHe.Trim(),
                TenBenh = tenBenh.Trim(),
                GhiChu = ghiChu?.Trim() ?? string.Empty
            });

            LogPatientAudit(patientId, "Thêm tiền sử gia đình", $"Thêm tiền sử gia đình cho bệnh nhân BN-{patientId:D4}: {quanHe} - {tenBenh}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm tiền sử gia đình.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // POST: Admin/Patients/AddImmunization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImmunization(int patientId, string tenVaccine, DateTime ngayTiem, string muiSo, string ghiChu)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (string.IsNullOrWhiteSpace(tenVaccine))
            {
                TempData["ErrorMessage"] = "Tên vắc-xin là bắt buộc.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            _context.Immunizations.Add(new Immunization
            {
                BenhNhanId = patientId,
                TenVaccine = tenVaccine.Trim(),
                NgayTiem = ngayTiem,
                MuiSo = muiSo?.Trim() ?? string.Empty,
                GhiChu = ghiChu?.Trim() ?? string.Empty
            });

            LogPatientAudit(patientId, "Thêm mũi tiêm chủng", $"Thêm mũi tiêm chủng '{tenVaccine}' cho bệnh nhân BN-{patientId:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm thông tin tiêm chủng.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // POST: Admin/Patients/CreateAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(int patientId, int khoaId, int bacSiId, DateTime ngayHen, string gioHen, string lyDoKham)
        {
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == bacSiId && d.KhoaId == khoaId && d.User.TrangThai == "Active");

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Bác sĩ hoặc khoa khám không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            if (string.IsNullOrWhiteSpace(gioHen) || !TimeSpan.TryParse(gioHen, out var timeOfDay))
            {
                TempData["ErrorMessage"] = "Khung giờ không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var appointmentTime = ngayHen.Date.Add(timeOfDay);
            if (appointmentTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể đặt lịch hẹn ở thời điểm đã qua.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var doctorSlotTaken = await _context.Appointments
                .AnyAsync(a => a.BacSiId == bacSiId && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");
            if (doctorSlotTaken)
            {
                TempData["ErrorMessage"] = "Bác sĩ đã có lịch hẹn khác vào đúng khung giờ này.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            _context.Appointments.Add(new Appointment
            {
                BenhNhanId = patientId,
                BacSiId = bacSiId,
                ThoiGian = appointmentTime,
                TrangThai = "ChoXacNhan",
                LyDoKham = string.IsNullOrWhiteSpace(lyDoKham) ? string.Empty : lyDoKham.Trim(),
                NgayTao = DateTime.Now
            });

            LogPatientAudit(patientId, "Đặt lịch hẹn", $"Đặt lịch hẹn cho bệnh nhân BN-{patientId:D4} với bác sĩ {doctor.User.HoTen} vào lúc {appointmentTime:HH:mm dd/MM/yyyy}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tạo lịch hẹn.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // POST: Admin/Patients/UploadAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(int patientId, IFormFile file)
        {
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh để tải lên.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension) || file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ JPG/PNG và dung lượng tối đa 5MB.";
                return RedirectToAction(nameof(Details), new { id = patientId });
            }

            var storageRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(storageRoot);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var storedPath = Path.Combine(storageRoot, storedName);
            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            patient.User.AnhDaiDien = storedName;
            _context.Entry(patient.User).State = EntityState.Modified;

            LogPatientAudit(patientId, "Cập nhật ảnh đại diện", $"Cập nhật ảnh đại diện cho bệnh nhân BN-{patientId:D4}.");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật ảnh đại diện.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        private void LogPatientAudit(int patientId, string hanhDong, string chiTiet)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = hanhDong,
                ChiTiet = chiTiet,
                DoiTuongLoai = "BenhNhan",
                DoiTuongId = patientId
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
