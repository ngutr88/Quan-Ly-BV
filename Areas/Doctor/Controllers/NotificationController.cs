using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Doctor/Notification?type=All
        [HttpGet]
        public async Task<IActionResult> Index(string type = "All")
        {
            var doctorUserId = GetCurrentUserId();

            var dbNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == doctorUserId)
                .OrderByDescending(n => n.NgayGui)
                .ToListAsync();

            var list = dbNotifications.Select(n =>
            {
                var model = new NotificationViewModel
                {
                    Id = n.Id,
                    NgayGui = n.NgayGui,
                    DaDoc = n.DaDoc
                };

                string text = n.NoiDung;
                if (text.StartsWith("["))
                {
                    int closeIndex = text.IndexOf(']');
                    if (closeIndex > 0)
                    {
                        model.Category = text.Substring(1, closeIndex - 1);
                        text = text.Substring(closeIndex + 1).Trim();
                    }
                }

                int pipeIndex = text.IndexOf('|');
                if (pipeIndex >= 0)
                {
                    model.Title = text.Substring(0, pipeIndex).Trim();
                    model.Body = text.Substring(pipeIndex + 1).Trim();
                }
                else
                {
                    model.Title = "Thông báo";
                    model.Body = text;
                }

                return model;
            }).ToList();

            if (!string.IsNullOrEmpty(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(n => n.Category.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.UnreadCount = dbNotifications.Count(n => !n.DaDoc);
            ViewBag.ActiveFilter = type;

            return View(list);
        }

        // GET: /Doctor/Notification/UnreadCount - polled by the bell after a
        // SignalR "NotificationCountChanged" signal.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var doctorUserId = GetCurrentUserId();
            var count = await QuanLyBenhVien.Helpers.NotificationCountHelper.GetUnreadCountAsync(_context, doctorUserId);
            return Json(new { count });
        }

        // POST: /Doctor/Notification/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Doctor/Notification/MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var doctorUserId = GetCurrentUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.NguoiDungId == doctorUserId);

            if (notification == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông báo." });
            }

            if (!notification.DaDoc)
            {
                notification.DaDoc = true;
                _context.Entry(notification).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: /Doctor/Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var doctorUserId = GetCurrentUserId();
            var unreadNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == doctorUserId && !n.DaDoc)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var n in unreadNotifications)
                {
                    n.DaDoc = true;
                    _context.Entry(n).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Category { get; set; } = "HeThong";
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime NgayGui { get; set; }
        public bool DaDoc { get; set; }
    }
}
