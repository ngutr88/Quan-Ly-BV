using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Logs
        public async Task<IActionResult> Index(string searchString, string roleFilter, string actionFilter)
        {
            var query = _context.AuditLogs
                .Include(l => l.User)
                .AsQueryable();

            // Search Filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.HanhDong.Contains(searchString) || 
                                         l.ChiTiet.Contains(searchString) || 
                                         (l.User != null && l.User.HoTen.Contains(searchString)));
            }

            // Role Filter
            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(l => l.User != null && l.User.VaiTro == roleFilter);
            }

            // Action Type Filter (HanhDong)
            if (!string.IsNullOrEmpty(actionFilter))
            {
                query = query.Where(l => l.HanhDong == actionFilter);
            }

            var logs = await query
                .OrderByDescending(l => l.ThoiGian)
                .Take(100) // Limit to latest 100 logs for performance
                .ToListAsync();

            // Distinct actions for dropdown filter
            ViewBag.ActionsList = await _context.AuditLogs
                .Select(l => l.HanhDong)
                .Distinct()
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.ActionFilter = actionFilter;

            return View(logs);
        }
    }
}
