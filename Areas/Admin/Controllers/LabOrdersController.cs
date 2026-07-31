using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    // Gộp chung luồng tiếp nhận/trả kết quả CLS + quản trị danh mục dịch vụ/bộ
    // chỉ định trong một controller, theo đúng cách MedicinesController đang
    // gộp nhiều luồng liên quan (batch/nhận/duyệt) thay vì tách nhỏ.
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LabOrdersController : Controller
    {
        private const int MaxResultFiles = 10;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly DoctorDashboardNotifier _notifier;

        public LabOrdersController(ApplicationDbContext context, IWebHostEnvironment environment, DoctorDashboardNotifier notifier)
        {
            _context = context;
            _environment = environment;
            _notifier = notifier;
        }

        // ================================================================
        // Danh sách & xử lý phiếu chỉ định
        // ================================================================

        // GET: Admin/LabOrders
        [HttpGet]
        public async Task<IActionResult> Index(string? trangThai, string? searchString, int page = 1)
        {
            var query = _context.LabOrderItems
                .Include(i => i.DichVu)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.Patient).ThenInclude(p => p.User)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.BacSiChiDinh).ThenInclude(d => d.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(i => i.TrangThai == trangThai);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(i =>
                    i.PhieuChiDinh.Patient.User.HoTen.Contains(searchString) ||
                    i.PhieuChiDinh.MaPhieu.Contains(searchString) ||
                    i.DichVu.TenDichVu.Contains(searchString));
            }

            var paged = await query
                .OrderByDescending(i => i.PhieuChiDinh.NgayChiDinh)
                .ToPagedListAsync(page, PagedList<LabOrderItem>.DefaultPageSize);

            ViewBag.TrangThaiFilter = trangThai;
            ViewBag.SearchString = searchString;
            ViewBag.CanProcess = await CurrentUserCanProcessLabOrdersAsync();

            return View(paged);
        }

        // POST: Admin/LabOrders/ReceiveOrderItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrderItem(int id)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý phiếu chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.LabOrderItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.TrangThai != "ChoThucHien")
            {
                TempData["ErrorMessage"] = "Chỉ có thể tiếp nhận dòng chỉ định đang ở trạng thái Chờ thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            item.TrangThai = "DangThucHien";
            item.NgayNhanThucHien = DateTime.Now;
            item.NguoiNhanId = GetCurrentUserId();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Tiếp nhận chỉ định CLS",
                ChiTiet = $"Tiếp nhận thực hiện dòng chỉ định #{item.Id}.",
                DoiTuongLoai = "ChiTietPhieuChiDinhCLS",
                DoiTuongId = item.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tiếp nhận thực hiện.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/LabOrders/CancelOrderItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderItem(int id, string lyDo)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý phiếu chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.LabOrderItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.TrangThai != "ChoThucHien" && item.TrangThai != "DangThucHien")
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy dòng chỉ định đang chờ hoặc đang thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            item.TrangThai = "DaHuy";
            item.LyDoHuy = lyDo.Trim();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Hủy chỉ định CLS",
                ChiTiet = $"Hủy dòng chỉ định #{item.Id}. Lý do: {item.LyDoHuy}",
                DoiTuongLoai = "ChiTietPhieuChiDinhCLS",
                DoiTuongId = item.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã hủy dòng chỉ định.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // Nhập / trả kết quả
        // ================================================================

        // GET: Admin/LabOrders/Report/5  (id = ChiTietPhieuChiDinhCLS.Id)
        [HttpGet]
        public async Task<IActionResult> Report(int id)
        {
            var item = await LoadItemForReportAsync(id);
            if (item == null) return NotFound();

            if (item.TrangThai != "DangThucHien" && item.TrangThai != "DaCoKetQua")
            {
                TempData["ErrorMessage"] = "Chỉ có thể nhập kết quả cho dòng chỉ định đang thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanProcess = await CurrentUserCanProcessLabOrdersAsync();
            return View(item);
        }

        // POST: Admin/LabOrders/Report/5  (lưu nháp kết luận - CHƯA đổi trạng thái)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(int id, string ketLuan, bool coBatThuong, string? nguoiDuyetTen, List<IFormFile>? files)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý phiếu chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Report), new { id });
            }

            var item = await LoadItemForReportAsync(id);
            if (item == null) return NotFound();

            if (item.TrangThai != "DangThucHien")
            {
                TempData["ErrorMessage"] = "Chỉ có thể lưu kết quả cho dòng chỉ định đang thực hiện (chưa phát hành).";
                return RedirectToAction(nameof(Report), new { id });
            }

            if (string.IsNullOrWhiteSpace(ketLuan))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập kết luận.";
                return RedirectToAction(nameof(Report), new { id });
            }

            var existingFileCount = item.KetQua?.Files.Count ?? 0;
            var incomingFiles = (files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();

            if (existingFileCount + incomingFiles.Count > MaxResultFiles)
            {
                TempData["ErrorMessage"] = $"Mỗi kết quả tối đa {MaxResultFiles} file đính kèm.";
                return RedirectToAction(nameof(Report), new { id });
            }

            foreach (var file in incomingFiles)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension) || file.Length > MaxFileSizeBytes)
                {
                    TempData["ErrorMessage"] = "Chỉ hỗ trợ PDF/JPG/PNG và dung lượng tối đa 10MB mỗi file.";
                    return RedirectToAction(nameof(Report), new { id });
                }
            }

            var result = item.KetQua;
            if (result == null)
            {
                result = new LabResult
                {
                    ChiTietPhieuChiDinhCLSId = item.Id,
                    NguoiThucHienId = GetCurrentUserId()
                };
                _context.LabResults.Add(result);
            }

            result.KetLuan = ketLuan.Trim();
            result.CoBatThuong = coBatThuong;
            result.NguoiDuyetTen = string.IsNullOrWhiteSpace(nguoiDuyetTen) ? null : nguoiDuyetTen.Trim();
            result.NgayTra = DateTime.Now;

            await _context.SaveChangesAsync(); // đảm bảo result.Id tồn tại cho file mới

            if (incomingFiles.Count > 0)
            {
                var storageRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "lab-results");
                Directory.CreateDirectory(storageRoot);
                var nextOrder = existingFileCount;

                foreach (var file in incomingFiles)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var storedName = $"{Guid.NewGuid():N}{extension}";
                    var storedPath = Path.Combine(storageRoot, storedName);
                    await using (var stream = System.IO.File.Create(storedPath))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.LabResultFiles.Add(new LabResultFile
                    {
                        KetQuaCLSId = result.Id,
                        TenGoc = Path.GetFileName(file.FileName),
                        TenLuuTru = storedName,
                        ContentType = file.ContentType ?? "application/octet-stream",
                        KichThuoc = file.Length,
                        ThuTu = nextOrder++
                    });
                }
            }

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Lưu kết quả CLS (nháp)",
                ChiTiet = $"Lưu kết luận cho dòng chỉ định #{item.Id}, {incomingFiles.Count} file mới. Chưa phát hành cho bác sĩ.",
                DoiTuongLoai = "ChiTietPhieuChiDinhCLS",
                DoiTuongId = item.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu kết quả (nháp) - bấm \"Phát hành\" để gửi cho bác sĩ.";
            return RedirectToAction(nameof(Report), new { id });
        }

        // POST: Admin/LabOrders/DeleteResultFile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResultFile(int fileId, int itemId)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý phiếu chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Report), new { id = itemId });
            }

            var file = await _context.LabResultFiles
                .Include(f => f.KetQua).ThenInclude(r => r.ChiTietPhieuChiDinh)
                .FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return NotFound();

            if (file.KetQua.ChiTietPhieuChiDinh.TrangThai != "DangThucHien")
            {
                TempData["ErrorMessage"] = "Không thể xóa file đính kèm sau khi đã phát hành kết quả.";
                return RedirectToAction(nameof(Report), new { id = itemId });
            }

            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "lab-results", file.TenLuuTru);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            _context.LabResultFiles.Remove(file);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa file đính kèm.";
            return RedirectToAction(nameof(Report), new { id = itemId });
        }

        // GET: Admin/LabOrders/DownloadResultFile/5
        [HttpGet]
        public async Task<IActionResult> DownloadResultFile(int fileId)
        {
            var file = await _context.LabResultFiles.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return NotFound();

            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "lab-results", file.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, file.ContentType, file.TenGoc);
        }

        // POST: Admin/LabOrders/PublishResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResult(int id)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý phiếu chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Report), new { id });
            }

            var item = await _context.LabOrderItems
                .Include(i => i.KetQua)
                .Include(i => i.DichVu)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.Patient).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.TrangThai != "DangThucHien")
            {
                TempData["ErrorMessage"] = "Chỉ có thể phát hành kết quả cho dòng chỉ định đang thực hiện.";
                return RedirectToAction(nameof(Report), new { id });
            }

            if (item.KetQua == null || string.IsNullOrWhiteSpace(item.KetQua.KetLuan))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập và lưu kết luận trước khi phát hành.";
                return RedirectToAction(nameof(Report), new { id });
            }

            item.TrangThai = "DaCoKetQua";

            var doctorNguoiDungId = await _context.Doctors
                .Where(d => d.Id == item.PhieuChiDinh.BacSiChiDinhId)
                .Select(d => d.NguoiDungId)
                .FirstOrDefaultAsync();

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = doctorNguoiDungId,
                NoiDung = $"[KetQuaCLS] Có kết quả cận lâm sàng mới|Kết quả {item.DichVu.TenDichVu} của bệnh nhân {item.PhieuChiDinh.Patient.User.HoTen} đã có.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Phát hành kết quả CLS",
                ChiTiet = $"Phát hành kết quả dòng chỉ định #{item.Id} ({item.DichVu.TenDichVu}) cho bác sĩ.",
                DoiTuongLoai = "ChiTietPhieuChiDinhCLS",
                DoiTuongId = item.Id
            });

            await _context.SaveChangesAsync();
            await _notifier.NotifyLabResultsUpdatedAsync(item.PhieuChiDinh.BacSiChiDinhId);

            TempData["SuccessMessage"] = "Đã phát hành kết quả cho bác sĩ chỉ định.";
            return RedirectToAction(nameof(Report), new { id });
        }

        private async Task<LabOrderItem?> LoadItemForReportAsync(int id)
        {
            return await _context.LabOrderItems
                .Include(i => i.DichVu)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.Patient).ThenInclude(p => p.User)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.BacSiChiDinh).ThenInclude(d => d.User)
                .Include(i => i.KetQua).ThenInclude(r => r!.Files)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        // GET: Admin/LabOrders/PrintOrder/5  (id = PhieuChiDinhCLS.Id)
        [HttpGet]
        public async Task<IActionResult> PrintOrder(int labOrderId)
        {
            var order = await _context.LabOrders
                .Include(o => o.Patient).ThenInclude(p => p.User)
                .Include(o => o.BacSiChiDinh).ThenInclude(d => d.User)
                .Include(o => o.BoChiDinh)
                .Include(o => o.ChiTiet).ThenInclude(i => i.DichVu)
                .FirstOrDefaultAsync(o => o.Id == labOrderId);
            if (order == null) return NotFound();

            return View("~/Views/Shared/_LabOrderPrintDocument.cshtml", order);
        }

        // ================================================================
        // Danh mục dịch vụ CLS
        // ================================================================

        // GET: Admin/LabOrders/Catalog
        [HttpGet]
        public async Task<IActionResult> Catalog(string? nhom)
        {
            var query = _context.LabServiceCatalogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nhom)) query = query.Where(s => s.NhomCLS == nhom);

            var services = await query.OrderBy(s => s.NhomCLS).ThenBy(s => s.TenDichVu).ToListAsync();
            ViewBag.NhomFilter = nhom;
            ViewBag.CanProcess = await CurrentUserCanProcessLabOrdersAsync();
            return View(services);
        }

        // POST: Admin/LabOrders/CatalogCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CatalogCreate(string maDichVu, string tenDichVu, string nhomCLS, string? noiThucHien, decimal gia, string? ghiChu)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý danh mục CLS.";
                return RedirectToAction(nameof(Catalog));
            }

            if (string.IsNullOrWhiteSpace(maDichVu) || string.IsNullOrWhiteSpace(tenDichVu) || gia < 0)
            {
                TempData["ErrorMessage"] = "Mã dịch vụ, tên dịch vụ và giá hợp lệ là bắt buộc.";
                return RedirectToAction(nameof(Catalog));
            }

            if (await _context.LabServiceCatalogs.AnyAsync(s => s.MaDichVu == maDichVu.Trim()))
            {
                TempData["ErrorMessage"] = "Mã dịch vụ đã tồn tại.";
                return RedirectToAction(nameof(Catalog));
            }

            _context.LabServiceCatalogs.Add(new LabServiceCatalog
            {
                MaDichVu = maDichVu.Trim(),
                TenDichVu = tenDichVu.Trim(),
                NhomCLS = nhomCLS,
                NoiThucHien = string.IsNullOrWhiteSpace(noiThucHien) ? null : noiThucHien.Trim(),
                Gia = gia,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
                DangHoatDong = true
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm dịch vụ {tenDichVu}.";
            return RedirectToAction(nameof(Catalog));
        }

        // POST: Admin/LabOrders/CatalogEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CatalogEdit(int id, string tenDichVu, string nhomCLS, string? noiThucHien, decimal gia, string? ghiChu)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý danh mục CLS.";
                return RedirectToAction(nameof(Catalog));
            }

            var service = await _context.LabServiceCatalogs.FindAsync(id);
            if (service == null) return NotFound();

            if (string.IsNullOrWhiteSpace(tenDichVu) || gia < 0)
            {
                TempData["ErrorMessage"] = "Tên dịch vụ và giá hợp lệ là bắt buộc.";
                return RedirectToAction(nameof(Catalog));
            }

            service.TenDichVu = tenDichVu.Trim();
            service.NhomCLS = nhomCLS;
            service.NoiThucHien = string.IsNullOrWhiteSpace(noiThucHien) ? null : noiThucHien.Trim();
            service.Gia = gia;
            service.GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật dịch vụ {tenDichVu}.";
            return RedirectToAction(nameof(Catalog));
        }

        // POST: Admin/LabOrders/CatalogToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CatalogToggleActive(int id)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý danh mục CLS.";
                return RedirectToAction(nameof(Catalog));
            }

            var service = await _context.LabServiceCatalogs.FindAsync(id);
            if (service == null) return NotFound();

            service.DangHoatDong = !service.DangHoatDong;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = service.DangHoatDong ? "Đã bật lại dịch vụ." : "Đã ẩn dịch vụ khỏi danh mục chỉ định.";
            return RedirectToAction(nameof(Catalog));
        }

        // ================================================================
        // Bộ chỉ định
        // ================================================================

        // GET: Admin/LabOrders/Bundles
        [HttpGet]
        public async Task<IActionResult> Bundles()
        {
            var bundles = await _context.LabOrderBundles
                .Include(b => b.ThanhVien).ThenInclude(i => i.DichVu)
                .OrderBy(b => b.TenBo)
                .ToListAsync();

            ViewBag.AllServices = await _context.LabServiceCatalogs
                .Where(s => s.DangHoatDong)
                .OrderBy(s => s.NhomCLS).ThenBy(s => s.TenDichVu)
                .ToListAsync();
            ViewBag.CanProcess = await CurrentUserCanProcessLabOrdersAsync();
            return View(bundles);
        }

        // POST: Admin/LabOrders/BundleCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BundleCreate(string tenBo, string? moTa, List<int>? dichVuIds)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý bộ chỉ định.";
                return RedirectToAction(nameof(Bundles));
            }

            if (string.IsNullOrWhiteSpace(tenBo) || dichVuIds == null || dichVuIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Tên bộ và ít nhất một dịch vụ là bắt buộc.";
                return RedirectToAction(nameof(Bundles));
            }

            var bundle = new LabOrderBundle
            {
                TenBo = tenBo.Trim(),
                MoTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim(),
                DangHoatDong = true,
                ThanhVien = dichVuIds.Distinct().Select(dvId => new LabOrderBundleItem { DichVuCLSId = dvId }).ToList()
            };
            _context.LabOrderBundles.Add(bundle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo bộ chỉ định {tenBo}.";
            return RedirectToAction(nameof(Bundles));
        }

        // POST: Admin/LabOrders/BundleEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BundleEdit(int id, string tenBo, string? moTa, List<int>? dichVuIds)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý bộ chỉ định.";
                return RedirectToAction(nameof(Bundles));
            }

            var bundle = await _context.LabOrderBundles.Include(b => b.ThanhVien).FirstOrDefaultAsync(b => b.Id == id);
            if (bundle == null) return NotFound();

            if (string.IsNullOrWhiteSpace(tenBo) || dichVuIds == null || dichVuIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Tên bộ và ít nhất một dịch vụ là bắt buộc.";
                return RedirectToAction(nameof(Bundles));
            }

            bundle.TenBo = tenBo.Trim();
            bundle.MoTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();

            _context.LabOrderBundleItems.RemoveRange(bundle.ThanhVien);
            bundle.ThanhVien = dichVuIds.Distinct().Select(dvId => new LabOrderBundleItem { BoChiDinhCLSId = bundle.Id, DichVuCLSId = dvId }).ToList();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật bộ chỉ định {tenBo}.";
            return RedirectToAction(nameof(Bundles));
        }

        // POST: Admin/LabOrders/BundleToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BundleToggleActive(int id)
        {
            if (!await CurrentUserCanProcessLabOrdersAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý bộ chỉ định.";
                return RedirectToAction(nameof(Bundles));
            }

            var bundle = await _context.LabOrderBundles.FindAsync(id);
            if (bundle == null) return NotFound();

            bundle.DangHoatDong = !bundle.DangHoatDong;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = bundle.DangHoatDong ? "Đã bật lại bộ chỉ định." : "Đã ẩn bộ chỉ định.";
            return RedirectToAction(nameof(Bundles));
        }

        private async Task<bool> CurrentUserCanProcessLabOrdersAsync()
        {
            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            return currentUser?.DuocXuLyCLS == true;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
