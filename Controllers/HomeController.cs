using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models.ViewModels;

namespace QuanLyBenhVien.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (User.IsInRole("Doctor")) return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        await PopulateHospitalFactsAsync();

        var allDepartments = await _context.Departments
            .OrderBy(d => d.TenKhoa)
            .ToListAsync();

        // The grid previews a handful; the booking picker needs every option.
        ViewBag.AllDepartments = allDepartments;
        ViewBag.Departments = allDepartments.Take(8).ToList();

        // Lead with the most senior doctors so the public page shows real staff.
        ViewBag.FeaturedDoctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.User.TrangThai == "Active")
            .OrderByDescending(d => d.SoNamKinhNghiem)
            .Take(4)
            .ToListAsync();

        ViewBag.Services = await _context.Services
            .Include(s => s.Department)
            .OrderBy(s => s.Gia)
            .Take(6)
            .ToListAsync();

        ViewBag.LatestArticles = await _context.Articles
            .Where(a => a.DaXuatBan)
            .OrderByDescending(a => a.NoiBat)
            .ThenByDescending(a => a.NgayDang)
            .Take(3)
            .ToListAsync();

        ViewBag.Reviews = await _context.Reviews
            .Include(r => r.Patient.User)
            .Include(r => r.Doctor.User)
            .Where(r => r.SoSao >= 4 && r.NhanXet != null && r.NhanXet != "")
            .OrderByDescending(r => r.NgayTao)
            .Take(3)
            .ToListAsync();

        return View();
    }

    // GET: /Home/News
    public async Task<IActionResult> News(string category, int page = 1)
    {
        var query = _context.Articles.Where(a => a.DaXuatBan);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(a => a.ChuyenMuc == category);
        }

        const int pageSize = 6;
        var total = await query.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        var safePage = Math.Clamp(page < 1 ? 1 : page, 1, totalPages);

        ViewBag.Articles = await query
            .OrderByDescending(a => a.NoiBat)
            .ThenByDescending(a => a.NgayDang)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await _context.Articles
            .Where(a => a.DaXuatBan)
            .Select(a => a.ChuyenMuc)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.ActiveCategory = category;
        ViewBag.Page = safePage;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalArticles = total;

        return View();
    }

    // GET: /Home/NewsDetail/5
    public async Task<IActionResult> NewsDetail(int id)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id && a.DaXuatBan);
        if (article == null) return NotFound();

        // Cheap view counter; a failure here must never break the page.
        try
        {
            article.LuotXem++;
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Ignore: the article still renders without an accurate counter.
        }

        ViewBag.Related = await _context.Articles
            .Where(a => a.DaXuatBan && a.Id != article.Id && a.ChuyenMuc == article.ChuyenMuc)
            .OrderByDescending(a => a.NgayDang)
            .Take(3)
            .ToListAsync();

        return View(article);
    }

    public IActionResult Privacy() => View();

    public IActionResult About() => View();

    public async Task<IActionResult> Specialities()
    {
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync();

        // Doctor headcount per department, so the page reports real staffing.
        ViewBag.DoctorCounts = await _context.Doctors
            .Where(d => d.User.TrangThai == "Active")
            .GroupBy(d => d.KhoaId)
            .Select(g => new { KhoaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.KhoaId, x => x.Count);

        ViewBag.Services = await _context.Services
            .Include(s => s.Department)
            .OrderBy(s => s.Department.TenKhoa)
            .ThenBy(s => s.Gia)
            .ToListAsync();

        return View();
    }

    public IActionResult Features() => View();

    // GET: /Home/Doctors — public directory so visitors can choose a doctor
    // before creating an account.
    public async Task<IActionResult> Doctors(string searchString, int? departmentId, int page = 1)
    {
        var query = _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.User.TrangThai == "Active");

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(d => d.User.HoTen.Contains(searchString)
                                     || d.ChuyenKhoa.Contains(searchString)
                                     || d.Department.TenKhoa.Contains(searchString));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(d => d.KhoaId == departmentId.Value);
        }

        const int pageSize = 12;
        var total = await query.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        var safePage = Math.Clamp(page < 1 ? 1 : page, 1, totalPages);

        ViewBag.Doctors = await query
            .OrderByDescending(d => d.SoNamKinhNghiem)
            .ThenBy(d => d.User.HoTen)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync();
        ViewBag.SearchString = searchString;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Page = safePage;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalDoctors = total;

        return View();
    }

    // GET: /Home/Pricing — published service price list.
    public async Task<IActionResult> Pricing(string searchString, int? departmentId)
    {
        var query = _context.Services
            .Include(s => s.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(s => s.TenDichVu.Contains(searchString)
                                     || s.Department.TenKhoa.Contains(searchString));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.KhoaId == departmentId.Value);
        }

        var services = await query
            .OrderBy(s => s.Department.TenKhoa)
            .ThenBy(s => s.Gia)
            .ToListAsync();

        ViewBag.Services = services;
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync();
        ViewBag.SearchString = searchString;
        ViewBag.DepartmentId = departmentId;
        ViewBag.TotalServices = services.Count;
        ViewBag.MinPrice = services.Count > 0 ? services.Min(s => s.Gia) : 0m;
        ViewBag.MaxPrice = services.Count > 0 ? services.Max(s => s.Gia) : 0m;

        return View();
    }

    // GET: /Home/Guide — how to prepare for a visit, paperwork and FAQ.
    public async Task<IActionResult> Guide()
    {
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Testimonials()
    {
        ViewBag.Reviews = await _context.Reviews
            .Include(r => r.Patient.User)
            .Include(r => r.Doctor.User)
            .Include(r => r.Doctor.Department)
            .Where(r => r.NhanXet != null && r.NhanXet != "")
            .OrderByDescending(r => r.NgayTao)
            .Take(24)
            .ToListAsync();

        var scores = await _context.Reviews.Select(r => r.SoSao).ToListAsync();
        ViewBag.AverageRating = scores.Count > 0 ? scores.Average(s => (double)s) : 0d;
        ViewBag.ReviewCount = scores.Count;

        return View();
    }

    public IActionResult Contact() => View();

    /// <summary>
    /// Real counts for the headline figures, replacing hard-coded marketing
    /// numbers that did not match the hospital's actual data.
    /// </summary>
    private async Task PopulateHospitalFactsAsync()
    {
        ViewBag.DepartmentCount = await _context.Departments.CountAsync();
        ViewBag.DoctorCount = await _context.Doctors.CountAsync(d => d.User.TrangThai == "Active");
        ViewBag.CompletedVisits = await _context.Appointments.CountAsync(a => a.TrangThai == "HoanThanh");
        ViewBag.ServiceCount = await _context.Services.CountAsync();

        var reviews = await _context.Reviews.Select(r => r.SoSao).ToListAsync();
        ViewBag.AverageRating = reviews.Count > 0 ? reviews.Average(s => (double)s) : 0d;
        ViewBag.ReviewCount = reviews.Count;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
