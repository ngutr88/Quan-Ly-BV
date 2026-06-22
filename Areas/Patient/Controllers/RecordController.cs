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
                .FirstOrDefaultAsync(p => p.Id == id && p.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

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
                .Select(e => new {
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

            ViewBag.Patient = patient;
            return View();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 3;
        }
    }
}
