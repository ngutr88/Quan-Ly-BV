using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models.ViewModels;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Doctor/Prescriptions
        public IActionResult Index() => RedirectToAction(nameof(List));

        // GET: /Doctor/Prescriptions/List
        public async Task<IActionResult> List(DateTime? from, DateTime? to, string? patientQuery, string? status, int page = 1, int? pageSize = null)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var query = _context.Prescriptions.AsNoTracking()
                .Where(p => p.BacSiKeId == doctorId.Value);

            if (from.HasValue) query = query.Where(p => p.NgayKe.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(p => p.NgayKe.Date <= to.Value.Date);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.TrangThai == status);
            if (!string.IsNullOrWhiteSpace(patientQuery))
            {
                var term = patientQuery.Trim().ToLower();
                query = query.Where(p => p.ExaminationRecord.Appointment.Patient.User.HoTen.ToLower().Contains(term));
            }

            var projected = query
                .OrderByDescending(p => p.NgayKe)
                .Select(p => new PrescriptionListItemViewModel
                {
                    Id = p.Id,
                    NgayKe = p.NgayKe,
                    PatientId = p.ExaminationRecord.Appointment.BenhNhanId,
                    PatientName = p.ExaminationRecord.Appointment.Patient.User.HoTen,
                    ChanDoan = p.ExaminationRecord.ChanDoan,
                    SoLoaiThuoc = p.PrescriptionDetails.Count,
                    TrangThai = p.TrangThai
                });

            // pageSize không truyền qua query string -> dùng tuỳ chọn "Số dòng
            // mỗi trang" lưu theo tài khoản (Doctor/Profile/SaveUiPreferences),
            // null (chưa từng đặt) mới rơi về mặc định hệ thống.
            var effectivePageSize = pageSize ?? await _context.Users
                .Where(u => u.Id == GetCurrentUserId())
                .Select(u => u.SoDongMoiTrangMacDinh)
                .FirstOrDefaultAsync() ?? PagedList<PrescriptionListItemViewModel>.DefaultPageSize;
            var paged = await projected.ToPagedListAsync(page, effectivePageSize);

            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.PatientQuery = patientQuery;
            ViewBag.Status = status;
            ViewBag.ActiveTab = "List";

            return View(paged);
        }

        // GET: /Doctor/Prescriptions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails).ThenInclude(d => d.Medicine)
                .Include(p => p.ExaminationRecord).ThenInclude(e => e.Appointment).ThenInclude(a => a.Patient.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.BacSiKeId == doctorId.Value);

            if (prescription == null) return NotFound();

            ViewBag.AuditEntries = await _context.AuditLogs.AsNoTracking()
                .Where(a => a.DoiTuongLoai == "DonThuoc" && a.DoiTuongId == id)
                .OrderByDescending(a => a.ThoiGian)
                .ToListAsync();
            ViewBag.ActiveTab = "List";

            return View(prescription);
        }

        // GET: /Doctor/Prescriptions/Templates - chưa xây (Sprint 3)
        public IActionResult Templates()
        {
            ViewBag.ActiveTab = "Templates";
            var comingSoon = new ComingSoonViewModel
            {
                Icon = "bookmark_added",
                Message = "Tính năng đang được phát triển.",
                SubMessage = "Mẫu đơn cá nhân và mẫu đơn khoa sẽ được xây ở đợt sau."
            };
            return View("ComingSoonTab", comingSoon);
        }

        // GET: /Doctor/Prescriptions/Feedback - chưa xây (Sprint 3)
        public IActionResult Feedback()
        {
            ViewBag.ActiveTab = "Feedback";
            var comingSoon = new ComingSoonViewModel
            {
                Icon = "forum",
                Message = "Tính năng đang được phát triển.",
                SubMessage = "Luồng phản hồi từ dược sĩ (nếu có) sẽ được xây ở đợt sau."
            };
            return View("ComingSoonTab", comingSoon);
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
