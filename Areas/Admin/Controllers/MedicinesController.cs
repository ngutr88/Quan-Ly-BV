using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MedicinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Medicines
        public async Task<IActionResult> Index(string searchString, bool? lowStock)
        {
            var query = _context.Medicines
                .Include(m => m.LoThuocs)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => m.TenThuoc.Contains(searchString) || m.HoatChat.Contains(searchString));
            }

            if (lowStock.HasValue && lowStock.Value)
            {
                query = query.Where(m => m.TonKho <= m.NguongToiThieu);
            }

            var medicines = await query.ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.LowStockOnly = lowStock ?? false;

            return View(medicines);
        }

        // GET: Admin/Medicines/Batches
        public async Task<IActionResult> Batches()
        {
            var batches = await _context.MedicineBatches
                .Include(b => b.Medicine)
                .OrderBy(b => b.HanSuDung)
                .ToListAsync();
            return View(batches);
        }

        // GET: Admin/Medicines/ReceiveBatch
        [HttpGet]
        public async Task<IActionResult> ReceiveBatch()
        {
            ViewBag.ThuocId = new SelectList(await _context.Medicines.ToListAsync(), "Id", "TenThuoc");
            return View();
        }

        // POST: Admin/Medicines/ReceiveBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveBatch(int thuocId, string soLo, DateTime hanSuDung, int soLuongNhap)
        {
            var medicine = await _context.Medicines.FindAsync(thuocId);
            if (medicine == null) return NotFound();

            if (soLuongNhap <= 0)
            {
                ModelState.AddModelError("", "Số lượng nhập kho phải lớn hơn 0.");
                ViewBag.ThuocId = new SelectList(await _context.Medicines.ToListAsync(), "Id", "TenThuoc", thuocId);
                return View();
            }

            if (hanSuDung <= DateTime.Today)
            {
                ModelState.AddModelError("", "Hạn sử dụng của thuốc phải ở trong tương lai.");
                ViewBag.ThuocId = new SelectList(await _context.Medicines.ToListAsync(), "Id", "TenThuoc", thuocId);
                return View();
            }

            // Create new batch
            var batch = new MedicineBatch
            {
                ThuocId = thuocId,
                SoLo = soLo,
                HanSuDung = hanSuDung,
                SoLuongNhap = soLuongNhap,
                SoLuongTon = soLuongNhap
            };
            _context.MedicineBatches.Add(batch);

            // Update medicine total stock count
            medicine.TonKho += soLuongNhap;
            _context.Entry(medicine).State = EntityState.Modified;

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Nhập kho thuốc",
                ChiTiet = $"Nhập lô mới {soLo} cho thuốc {medicine.TenThuoc}. Số lượng: {soLuongNhap} {medicine.DonViTinh}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã nhập kho thành công {soLuongNhap} {medicine.DonViTinh} thuốc {medicine.TenThuoc}.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
