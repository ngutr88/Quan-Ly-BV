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
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctor/History
        public async Task<IActionResult> Index(string searchString)
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

            var query = _context.ExaminationRecords
                .Include(e => e.Appointment.Patient.User)
                .Where(e => e.Appointment.BacSiId == doctor.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(e => e.Appointment.Patient.User.HoTen.Contains(searchString) || 
                                         e.Appointment.Patient.User.Sdt.Contains(searchString));
            }

            var records = await query
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            return View(records);
        }

        // GET: Doctor/History/RecordDetails/5
        public async Task<IActionResult> RecordDetails(int id)
        {
            var currentUserId = GetCurrentUserId();
            var doctorId = await _context.Doctors
                .Where(d => d.NguoiDungId == currentUserId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
            if (!doctorId.HasValue) return Forbid();

            // id is AppointmentId (LichKhamId)
            var recordQuery = _context.ExaminationRecords
                .Include(e => e.Appointment.Patient.User)
                .Include(e => e.Appointment.Doctor.User)
                .Include(e => e.Appointment.Doctor.Department)
                .Where(e => e.Appointment.BacSiId == doctorId.Value);

            // Route chuẩn dùng LichKhamId; fallback theo PhieuKham.Id để các liên kết cũ
            // và bookmark đã lưu trước đây vẫn mở đúng hồ sơ thuộc bác sĩ hiện tại.
            var record = await recordQuery.FirstOrDefaultAsync(e => e.LichKhamId == id)
                ?? await recordQuery.FirstOrDefaultAsync(e => e.Id == id);

            if (record == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ bệnh án hoặc hồ sơ không thuộc phạm vi phụ trách của bạn.";
                return RedirectToAction(nameof(Index));
            }

            // Fetch Prescription (if any)
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .ThenInclude(pd => pd.Medicine)
                .FirstOrDefaultAsync(p => p.PhieuKhamId == record.Id);

            // Fetch Invoice (if any)
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.PhieuKhamId == record.Id);

            ViewBag.Prescription = prescription;
            ViewBag.Invoice = invoice;

            return View(record);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
