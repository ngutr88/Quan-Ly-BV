using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public QueueController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctor/Queue
        public async Task<IActionResult> Index(int? id)
        {
            var doctorUserEmail = User.Identity?.Name;
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.HoTen == doctorUserEmail || d.User.Email == doctorUserEmail);

            if (doctor == null)
            {
                var currentUserId = GetCurrentUserId();
                doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);
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
            }

            ViewBag.DoctorProfile = doctor;
            ViewBag.Queue = queue;
            ViewBag.SelectedAppointment = selectedApp;
            ViewBag.LastRecord = lastRecord;
            ViewBag.HistoryExams = historyExams;

            return View();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 2;
        }
    }
}
