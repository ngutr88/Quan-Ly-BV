using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (User.IsInRole("Doctor")) return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
