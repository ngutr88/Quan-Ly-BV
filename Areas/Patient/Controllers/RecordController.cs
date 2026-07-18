using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class RecordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RecordController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Patient/Record
        public async Task<IActionResult> Index()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var examRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Include(e => e.Appointment.Doctor.Department)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            var examRecordIds = examRecords.Select(e => e.Id).ToList();
            var recordsWithPrescriptions = await _context.Prescriptions
                .Where(p => examRecordIds.Contains(p.PhieuKhamId))
                .Select(p => p.PhieuKhamId)
                .ToListAsync();

            ViewBag.RecordsWithPrescriptions = recordsWithPrescriptions;
            ViewBag.PatientDocuments = await _context.PatientDocuments
                .Where(d => d.BenhNhanId == patient.Id)
                .OrderByDescending(d => d.NgayTaiLen)
                .ToListAsync();
            return View(examRecords);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file, string tenTaiLieu, string ghiChu)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension) || file.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ PDF/JPG/PNG và dung lượng tối đa 10MB.";
                return RedirectToAction(nameof(Index));
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
                BenhNhanId = patient.Id,
                TenTaiLieu = string.IsNullOrWhiteSpace(tenTaiLieu) ? Path.GetFileName(file.FileName) : tenTaiLieu.Trim(),
                LoaiTaiLieu = "GiayToKhamBenh",
                TenLuuTru = storedName,
                ContentType = file.ContentType ?? "application/octet-stream",
                KichThuoc = file.Length,
                GhiChu = ghiChu?.Trim() ?? string.Empty
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu tài liệu khám bệnh.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id && d.BenhNhanId == patient.Id);
            return await ServeDocumentAsync(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id && d.BenhNhanId == patient.Id);
            if (document != null)
            {
                var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                _context.PatientDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ServeDocumentAsync(PatientDocument? document)
        {
            if (document == null) return NotFound();
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, document.ContentType, document.TenTaiLieu);
        }

        private async Task<QuanLyBenhVien.Models.Patient?> GetCurrentPatientAsync()
        {
            var userId = GetCurrentUserId();
            return await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == userId);
        }

        // GET: /Patient/Record/PrescriptionDetails/5
        public async Task<IActionResult> PrescriptionDetails(int id)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .ThenInclude(pd => pd.Medicine)
                .Include(p => p.ExaminationRecord.Appointment.Doctor.User)
                .Include(p => p.ExaminationRecord.Appointment.Doctor.Department)
                .FirstOrDefaultAsync(p => p.PhieuKhamId == id && p.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (prescription == null) return NotFound("Không tìm thấy đơn thuốc hoặc bạn không có quyền truy cập.");

            return View(prescription);
        }

        // GET: /Patient/Record/Health
        [HttpGet]
        public async Task<IActionResult> Health()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            // Fetch vital signs from past examination records to plot trends
            var healthLogs = await _context.ExaminationRecords
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderBy(e => e.NgayKham)
                .Select(e => new
                {
                    date = e.NgayKham.ToString("dd/MM/yyyy"),
                    weight = e.CanNang,
                    height = e.ChieuCao,
                    bmi = e.BMI,
                    bp = e.HuyetAp,
                    hr = e.NhipTim
                })
                .ToListAsync();

            ViewBag.Patient = patient;
            ViewBag.HealthLogsJson = System.Text.Json.JsonSerializer.Serialize(healthLogs);

            return View(patient);
        }

        // GET: /Patient/Record/Dependents
        [HttpGet]
        public async Task<IActionResult> Dependents()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var dependents = await _context.Dependents
                .Where(d => d.BenhNhanId == patient.Id)
                .ToListAsync();

            ViewBag.Patient = patient;
            return View(dependents);
        }

        // POST: /Patient/Record/AddDependent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependent(string name, string relation, string gender, int birthYear, string blood, string bhyt, string history)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return Json(new { success = false, message = "Không tìm thấy bệnh nhân." });

            var dep = new Dependent
            {
                BenhNhanId = patient.Id,
                HoTen = name,
                QuanHe = relation,
                GioiTinh = gender,
                NamSinh = birthYear,
                NhomMau = string.IsNullOrWhiteSpace(blood) ? "O+" : blood,
                SoBHYT = bhyt ?? string.Empty,
                TienSuBenhLy = history ?? string.Empty
            };

            _context.Dependents.Add(dep);
            await _context.SaveChangesAsync();

            return Json(new { success = true, name = dep.HoTen });
        }

        // GET: /Patient/Record/GetReview
        [HttpGet]
        public async Task<IActionResult> GetReview(int doctorId)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BenhNhanId == patient.Id && r.BacSiId == doctorId);

            if (review == null)
            {
                return Json(new { exists = false });
            }

            return Json(new { exists = true, rating = review.SoSao, comment = review.NhanXet });
        }

        // POST: /Patient/Record/SubmitReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int doctorId, int rating, string comment)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return Json(new { success = false, message = "Bệnh nhân không tồn tại." });

            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == doctorId);
            if (!doctorExists) return Json(new { success = false, message = "Bác sĩ không tồn tại." });

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BenhNhanId == patient.Id && r.BacSiId == doctorId);

            if (existingReview != null)
            {
                existingReview.SoSao = rating;
                existingReview.NhanXet = comment ?? string.Empty;
                existingReview.NgayTao = DateTime.Now;
                _context.Entry(existingReview).State = EntityState.Modified;
            }
            else
            {
                var review = new Review
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctorId,
                    SoSao = rating,
                    NhanXet = comment ?? string.Empty,
                    NgayTao = DateTime.Now
                };
                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
