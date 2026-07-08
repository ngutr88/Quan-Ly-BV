using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public RecordController(ApplicationDbContext context)
        {
            _context = context;
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
            return View(examRecords);
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
            return claim != null ? int.Parse(claim.Value) : 3;
        }
    }
}
