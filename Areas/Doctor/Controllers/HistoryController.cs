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
        private const string BreakTheGlassAction = "BreakTheGlass_XemHoSoBenhNhan";
        private const int BreakTheGlassGrantMinutes = 2;

        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctor/History/QuickSearch?q=... - backs the topbar search box.
        // Scope is "lịch hẹn của mình" (appointments), not just completed
        // records, so an upcoming-but-not-yet-seen patient is still findable.
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(Array.Empty<object>());

            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var term = q.Trim();

            var inScopePatientIds = (await _context.Appointments
                .Where(a => a.BacSiId == doctorId.Value)
                .Select(a => a.BenhNhanId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var matches = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.User.HoTen.Contains(term) || p.User.Sdt.Contains(term))
                .OrderBy(p => p.User.HoTen)
                .Take(10)
                .ToListAsync();

            var patientIds = matches.Select(p => p.Id).ToList();

            // Most recent ExaminationRecord per patient (across ALL doctors) -
            // gives break-the-glass a concrete record to open for an
            // out-of-scope match. Ordered/grouped in-memory after a single
            // small query instead of a per-patient round trip.
            var latestLichKhamByPatient = (await _context.ExaminationRecords
                .Where(e => patientIds.Contains(e.Appointment.BenhNhanId))
                .OrderByDescending(e => e.NgayKham)
                .Select(e => new { e.Appointment.BenhNhanId, e.LichKhamId })
                .ToListAsync())
                .GroupBy(x => x.BenhNhanId)
                .ToDictionary(g => g.Key, g => g.First().LichKhamId);

            var result = matches.Select(p => new
            {
                patientId = p.Id,
                patientName = p.User.HoTen,
                phone = p.User.Sdt,
                inScope = inScopePatientIds.Contains(p.Id),
                latestLichKhamId = latestLichKhamByPatient.TryGetValue(p.Id, out var lichKhamId) ? lichKhamId : (int?)null
            });

            return Json(result);
        }

        // POST: Doctor/History/BreakTheGlass - logs a justified out-of-scope
        // access request. The AuditLog row this writes IS the grant checked
        // by RecordDetails() above - no separate flag/table.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BreakTheGlass(int patientId, string reason)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do truy cập đủ rõ ràng (tối thiểu 5 ký tự)." });
            }

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId);
            if (!patientExists) return NotFound();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = BreakTheGlassAction,
                ChiTiet = reason.Trim(),
                DoiTuongLoai = "BenhNhan",
                DoiTuongId = patientId
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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
        public async Task<IActionResult> RecordDetails(int id, bool breakGlass = false)
        {
            var currentUserId = GetCurrentUserId();
            var doctorId = await _context.Doctors
                .Where(d => d.NguoiDungId == currentUserId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
            if (!doctorId.HasValue) return Forbid();

            // Not scoped to BacSiId at the query level here (unlike Index()) -
            // a break-the-glass grant can allow reading a record outside this
            // doctor's own patients, checked explicitly below instead.
            var recordQuery = _context.ExaminationRecords
                .Include(e => e.Appointment.Patient.User)
                .Include(e => e.Appointment.Doctor.User)
                .Include(e => e.Appointment.Doctor.Department);

            // Route chuẩn dùng LichKhamId; fallback theo PhieuKham.Id để các liên kết cũ
            // và bookmark đã lưu trước đây vẫn mở đúng hồ sơ thuộc bác sĩ hiện tại.
            var record = await recordQuery.FirstOrDefaultAsync(e => e.LichKhamId == id)
                ?? await recordQuery.FirstOrDefaultAsync(e => e.Id == id);

            const string notFoundOrOutOfScopeMessage = "Không tìm thấy hồ sơ bệnh án hoặc hồ sơ không thuộc phạm vi phụ trách của bạn.";

            if (record == null)
            {
                TempData["ErrorMessage"] = notFoundOrOutOfScopeMessage;
                return RedirectToAction(nameof(Index));
            }

            if (record.Appointment.BacSiId != doctorId.Value)
            {
                // Out of scope: only allowed through a recent break-the-glass
                // grant (the AuditLog row itself IS the grant - see
                // BreakTheGlass() below, no separate flag/column needed).
                var hasGrant = breakGlass && await _context.AuditLogs.AnyAsync(a =>
                    a.NguoiDungId == currentUserId &&
                    a.HanhDong == BreakTheGlassAction &&
                    a.DoiTuongLoai == "BenhNhan" &&
                    a.DoiTuongId == record.Appointment.BenhNhanId &&
                    a.ThoiGian >= DateTime.Now.AddMinutes(-BreakTheGlassGrantMinutes));

                if (!hasGrant)
                {
                    TempData["ErrorMessage"] = notFoundOrOutOfScopeMessage;
                    return RedirectToAction(nameof(Index));
                }
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
