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
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Notification?type=All
        [HttpGet]
        public async Task<IActionResult> Index(string type = "All")
        {
            var patientUserId = GetCurrentUserId();

            var dbNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == patientUserId)
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
                // Parse Category
                if (text.StartsWith("["))
                {
                    int closeIndex = text.IndexOf(']');
                    if (closeIndex > 0)
                    {
                        model.Category = text.Substring(1, closeIndex - 1);
                        text = text.Substring(closeIndex + 1).Trim();
                    }
                }

                // Parse Title & Body
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

            // Filter in memory based on parsed Category
            if (!string.IsNullOrEmpty(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(n => n.Category.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Unread counts for the view badge
            ViewBag.UnreadCount = dbNotifications.Count(n => !n.DaDoc);
            ViewBag.ActiveFilter = type;

            return View(list);
        }

        // POST: /Patient/Notification/MarkAsRead/5
        [HttpPost]
        [Route("Patient/Notification/MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var patientUserId = GetCurrentUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.NguoiDungId == patientUserId);

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

        // POST: /Patient/Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var patientUserId = GetCurrentUserId();
            var unreadNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == patientUserId && !n.DaDoc)
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
