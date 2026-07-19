using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public static readonly string[] Categories =
        { "Tin tức", "Thông báo", "Sức khỏe", "Tuyển dụng" };

    // GET: Admin/News
    public async Task<IActionResult> Index(string searchString, string category, int page = 1)
    {
        var query = _context.Articles.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(a => a.TieuDe.Contains(searchString) || a.TomTat.Contains(searchString));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(a => a.ChuyenMuc == category);
        }

        var paged = await query
            .OrderByDescending(a => a.NgayDang)
            .ToPagedListAsync(page, PagedList<Article>.DefaultPageSize);

        ViewBag.SearchString = searchString;
        ViewBag.Category = category;
        ViewBag.Categories = Categories;
        ViewBag.TotalMatching = paged.TotalCount;
        ViewBag.PublishedCount = await query.CountAsync(a => a.DaXuatBan);
        ViewBag.DraftCount = await query.CountAsync(a => !a.DaXuatBan);
        ViewBag.TotalViews = await query.SumAsync(a => (int?)a.LuotXem) ?? 0;

        return View(paged);
    }

    // GET: Admin/News/Create
    public IActionResult Create()
    {
        ViewBag.Categories = Categories;
        return View(new Article { NgayDang = DateTime.Now });
    }

    // POST: Admin/News/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article article)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = Categories;
            return View(article);
        }

        article.NgayDang = article.NgayDang == default ? DateTime.Now : article.NgayDang;
        article.LuotXem = 0;

        _context.Articles.Add(article);
        _context.AuditLogs.Add(new AuditLog
        {
            NguoiDungId = GetCurrentUserId(),
            HanhDong = "Đăng bài viết",
            ChiTiet = $"Tạo bài viết \"{article.TieuDe}\" thuộc chuyên mục {article.ChuyenMuc}."
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã đăng bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/News/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();

        ViewBag.Categories = Categories;
        return View(article);
    }

    // POST: Admin/News/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Article article)
    {
        if (id != article.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = Categories;
            return View(article);
        }

        var existing = await _context.Articles.FindAsync(id);
        if (existing == null) return NotFound();

        existing.TieuDe = article.TieuDe;
        existing.TomTat = article.TomTat;
        existing.NoiDung = article.NoiDung;
        existing.ChuyenMuc = article.ChuyenMuc;
        existing.AnhBia = article.AnhBia ?? string.Empty;
        existing.TacGia = article.TacGia ?? string.Empty;
        existing.DaXuatBan = article.DaXuatBan;
        existing.NoiBat = article.NoiBat;
        existing.NgayCapNhat = DateTime.Now;
        // View count and original publish date are not editable from the form.

        _context.AuditLogs.Add(new AuditLog
        {
            NguoiDungId = GetCurrentUserId(),
            HanhDong = "Sửa bài viết",
            ChiTiet = $"Cập nhật bài viết \"{existing.TieuDe}\" (ID {existing.Id})."
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật bài viết.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/News/TogglePublish/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();

        article.DaXuatBan = !article.DaXuatBan;
        article.NgayCapNhat = DateTime.Now;

        _context.AuditLogs.Add(new AuditLog
        {
            NguoiDungId = GetCurrentUserId(),
            HanhDong = article.DaXuatBan ? "Xuất bản bài viết" : "Ẩn bài viết",
            ChiTiet = $"Bài viết \"{article.TieuDe}\" (ID {article.Id})."
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = article.DaXuatBan
            ? "Bài viết đã được xuất bản."
            : "Bài viết đã được chuyển về bản nháp.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/News/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();

        _context.Articles.Remove(article);
        _context.AuditLogs.Add(new AuditLog
        {
            NguoiDungId = GetCurrentUserId(),
            HanhDong = "Xóa bài viết",
            ChiTiet = $"Xóa bài viết \"{article.TieuDe}\" (ID {article.Id})."
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa bài viết.";
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
    }
}
