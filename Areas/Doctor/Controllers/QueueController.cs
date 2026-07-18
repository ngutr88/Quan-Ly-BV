using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class QueueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public QueueController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Doctor/Queue
        public async Task<IActionResult> Index(int? id)
        {
            var currentUserId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);

            if (doctor == null)
            {
                var identityValue = User.Identity?.Name;
                doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.HoTen == identityValue || d.User.Email == identityValue);
            }

            if (doctor == null) return NotFound("Bác sĩ không tồn tại trong hệ thống.");

            // Today's Queue list (excluding canceled)
            var queue = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctor.Id && a.ThoiGian.Date == DateTime.Today && a.TrangThai != "DaHuy")
                .OrderBy(a => a.ThoiGian)
                .ToListAsync();

            Appointment? selectedApp = null;
            ExaminationRecord? lastRecord = null;
            var historyExams = new System.Collections.Generic.List<ExaminationRecord>();
            var patientDocuments = new System.Collections.Generic.List<PatientDocument>();

            if (queue.Any())
            {
                int selectedId = id ?? queue.First().Id;
                selectedApp = queue.FirstOrDefault(a => a.Id == selectedId) ?? queue.First();

                // Fetch patient's last completed exam record for vital signs baseline
                lastRecord = await _context.ExaminationRecords
                    .Include(e => e.Appointment.Doctor.User)
                    .Where(e => e.Appointment.BenhNhanId == selectedApp.BenhNhanId)
                    .OrderByDescending(e => e.NgayKham)
                    .FirstOrDefaultAsync();

                // Fetch up to 3 past completed exam records for timeline
                historyExams = await _context.ExaminationRecords
                    .Include(e => e.Appointment.Doctor.User)
                    .Where(e => e.Appointment.BenhNhanId == selectedApp.BenhNhanId && e.Appointment.TrangThai == "HoanThanh")
                    .OrderByDescending(e => e.NgayKham)
                    .Take(3)
                    .ToListAsync();

                patientDocuments = await _context.PatientDocuments
                    .Where(d => d.BenhNhanId == selectedApp.BenhNhanId)
                    .OrderByDescending(d => d.NgayTaiLen)
                    .ToListAsync();
            }

            ViewBag.DoctorProfile = doctor;
            ViewBag.Queue = queue;
            ViewBag.SelectedAppointment = selectedApp;
            ViewBag.LastRecord = lastRecord;
            ViewBag.HistoryExams = historyExams;
            ViewBag.PatientDocuments = patientDocuments;
            ViewBag.SelectedHasRecord = selectedApp != null && await _context.ExaminationRecords
                .AnyAsync(e => e.LichKhamId == selectedApp.Id);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPatientDocument(int id)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == GetCurrentUserId());
            if (doctor == null) return Forbid();

            var document = await _context.PatientDocuments
                .Include(d => d.Patient)
                .FirstOrDefaultAsync(d => d.Id == id && _context.Appointments.Any(a =>
                    a.BenhNhanId == d.BenhNhanId && a.BacSiId == doctor.Id));
            if (document == null) return NotFound();

            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, document.ContentType, document.TenTaiLieu);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
