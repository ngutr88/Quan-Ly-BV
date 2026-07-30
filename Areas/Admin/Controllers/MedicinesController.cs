using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Models.ViewModels;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MedicinesController : Controller
    {
        // Hạn dùng dưới ngưỡng này (kể từ hôm nay) bị coi là "cận date" và cần
        // xác nhận riêng trước khi cho nhập (Yêu cầu 3).
        private const int CanDateThresholdMonths = 6;

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
                .Include(b => b.NhaCungCap)
                .OrderBy(b => b.HanSuDung)
                .ToListAsync();

            // Cho phép view liên kết ngược "xem phiếu nhập" cho các lô được
            // tạo ra từ phiếu nhập kho (LoThuocId trên chi tiết phiếu).
            var batchIds = batches.Select(b => b.Id).ToList();
            ViewBag.PhieuNhapTheoLo = await _context.GoodsReceiptDetails
                .Where(d => d.LoThuocId != null && batchIds.Contains(d.LoThuocId.Value))
                .Select(d => new { d.LoThuocId, d.PhieuNhapKhoId, MaPhieu = d.PhieuNhapKho.MaPhieu })
                .ToDictionaryAsync(
                    x => x.LoThuocId!.Value,
                    x => new BatchReceiptLink { PhieuNhapKhoId = x.PhieuNhapKhoId, MaPhieu = x.MaPhieu });

            return View(batches);
        }

        // ================================================================
        // Phiếu nhập kho: tạo mới / chỉnh sửa (Nháp, Từ chối)
        // ================================================================

        // GET: Admin/Medicines/ReceiveBatch[/5]
        [HttpGet]
        public async Task<IActionResult> ReceiveBatch(int? id, int? phieuGocId)
        {
            var vm = new GoodsReceiptViewModel();

            if (id.HasValue)
            {
                var receipt = await _context.GoodsReceipts
                    .Include(r => r.ChiTiet)
                    .FirstOrDefaultAsync(r => r.Id == id.Value);
                if (receipt == null) return NotFound();

                if (receipt.TrangThai != "Nhap" && receipt.TrangThai != "TuChoi")
                {
                    TempData["ErrorMessage"] = "Phiếu này đã khóa (đang chờ duyệt/đã duyệt/đã hủy), không thể chỉnh sửa.";
                    return RedirectToAction(nameof(ReceiptDetails), new { id = id.Value });
                }

                vm.Id = receipt.Id;
                vm.PhieuGocId = receipt.PhieuGocId;
                vm.MaPhieu = receipt.MaPhieu;
                vm.NgayNhap = receipt.NgayNhap;
                vm.LoaiNhap = receipt.LoaiNhap;
                vm.NhaCungCapId = receipt.NhaCungCapId;
                vm.SoHoaDonNCC = receipt.SoHoaDonNCC;
                vm.NgayHoaDon = receipt.NgayHoaDon;
                vm.KhoNhap = receipt.KhoNhap;
                vm.NguoiGiaoHang = receipt.NguoiGiaoHang;
                vm.GhiChu = receipt.GhiChu;
                vm.TrangThai = receipt.TrangThai;
                vm.ChiTiet = receipt.ChiTiet.Select(d => new GoodsReceiptDetailViewModel
                {
                    Id = d.Id,
                    ThuocId = d.ThuocId,
                    SoLo = d.SoLo,
                    HanSuDung = d.HanSuDung,
                    SoLuong = d.SoLuong,
                    DonGia = d.DonGia,
                    PhanTramVAT = d.PhanTramVAT,
                    XacNhanCanDate = d.XacNhanCanDate,
                    CongDonVaoLoHienCo = d.CongDonVaoLoHienCo
                }).ToList();

                var medicineIds = vm.ChiTiet.Select(c => c.ThuocId).Distinct().ToList();
                ViewBag.ExistingMedicines = await _context.Medicines
                    .Where(m => medicineIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id);
            }
            else
            {
                vm.MaPhieu = GenerateNextMaPhieu(DateTime.Now.Year);

                if (phieuGocId.HasValue)
                {
                    var goc = await _context.GoodsReceipts
                        .FirstOrDefaultAsync(r => r.Id == phieuGocId.Value && r.TrangThai == "DaDuyet");
                    if (goc != null)
                    {
                        vm.PhieuGocId = goc.Id;
                        ViewBag.PhieuGocMaPhieu = goc.MaPhieu;
                    }
                }
            }

            ViewBag.IsPreviewMaPhieu = !id.HasValue;
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.DangHoatDong)
                .OrderBy(s => s.TenNhaCungCap)
                .ToListAsync();
            ViewBag.CanApprove = await CurrentUserCanApproveAsync();

            return View(vm);
        }

        // POST: Admin/Medicines/SaveDraft  (nút "Lưu nháp")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(GoodsReceiptViewModel model)
        {
            var result = await SaveReceiptAsync(model, "Nhap");
            return Json(result);
        }

        // POST: Admin/Medicines/SubmitReceipt  (nút "Gửi duyệt")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReceipt(GoodsReceiptViewModel model)
        {
            var result = await SaveReceiptAsync(model, "ChoDuyet");
            return Json(result);
        }

        private async Task<GoodsReceiptActionResult> SaveReceiptAsync(GoodsReceiptViewModel model, string targetStatus)
        {
            var errors = await ValidateReceiptAsync(model);
            if (errors.Count > 0)
            {
                return new GoodsReceiptActionResult { Success = false, Errors = errors };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            GoodsReceipt receipt;
            var isNew = !model.Id.HasValue;

            if (isNew)
            {
                receipt = new GoodsReceipt
                {
                    MaPhieu = GenerateNextMaPhieu(model.NgayNhap.Year),
                    NguoiTaoId = GetCurrentUserId(),
                    PhieuGocId = model.PhieuGocId
                };
                _context.GoodsReceipts.Add(receipt);
            }
            else
            {
                var existing = await _context.GoodsReceipts
                    .Include(r => r.ChiTiet)
                    .FirstOrDefaultAsync(r => r.Id == model.Id!.Value);
                if (existing == null)
                {
                    return new GoodsReceiptActionResult
                    {
                        Success = false,
                        Errors = { new FieldError("header", "Không tìm thấy phiếu nhập kho.") }
                    };
                }

                if (existing.TrangThai != "Nhap" && existing.TrangThai != "TuChoi")
                {
                    return new GoodsReceiptActionResult
                    {
                        Success = false,
                        Errors = { new FieldError("header", "Phiếu đã gửi duyệt/đã duyệt/đã hủy, không thể chỉnh sửa.") }
                    };
                }

                receipt = existing;
                // Form luôn gửi lại toàn bộ bảng chi tiết - xóa dòng cũ rồi
                // thêm lại theo đúng danh sách mới nhất từ client.
                _context.GoodsReceiptDetails.RemoveRange(receipt.ChiTiet);
                receipt.NgayCapNhat = DateTime.Now;
            }

            receipt.NgayNhap = model.NgayNhap;
            receipt.LoaiNhap = model.LoaiNhap;
            receipt.NhaCungCapId = model.NhaCungCapId;
            receipt.SoHoaDonNCC = model.SoHoaDonNCC;
            receipt.NgayHoaDon = model.NgayHoaDon;
            receipt.KhoNhap = model.KhoNhap;
            receipt.NguoiGiaoHang = model.NguoiGiaoHang;
            receipt.GhiChu = model.GhiChu;
            receipt.TrangThai = targetStatus;

            receipt.ChiTiet.Clear();
            foreach (var line in model.ChiTiet)
            {
                var thanhTien = Math.Round(line.SoLuong * line.DonGia * (1 + line.PhanTramVAT / 100m), 2);
                receipt.ChiTiet.Add(new GoodsReceiptDetail
                {
                    ThuocId = line.ThuocId,
                    SoLo = line.SoLo.Trim(),
                    HanSuDung = line.HanSuDung!.Value.Date,
                    SoLuong = line.SoLuong,
                    DonGia = line.DonGia,
                    PhanTramVAT = line.PhanTramVAT,
                    ThanhTien = thanhTien,
                    XacNhanCanDate = line.XacNhanCanDate,
                    CongDonVaoLoHienCo = line.CongDonVaoLoHienCo
                });
            }

            ApplyTotals(receipt, model.ChiTiet);

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = isNew
                    ? "Tạo phiếu nhập kho"
                    : (targetStatus == "ChoDuyet" ? "Gửi duyệt phiếu nhập kho" : "Cập nhật phiếu nhập kho"),
                ChiTiet = $"Phiếu {receipt.MaPhieu}, {model.ChiTiet.Count} dòng thuốc, tổng thanh toán dự kiến {receipt.TongThanhToan:N0}đ.",
                DoiTuongLoai = "PhieuNhapKho",
                DoiTuongId = receipt.Id
            });
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new GoodsReceiptActionResult
            {
                Success = true,
                Id = receipt.Id,
                MaPhieu = receipt.MaPhieu,
                Message = targetStatus == "ChoDuyet"
                    ? $"Đã gửi duyệt phiếu {receipt.MaPhieu}."
                    : $"Đã lưu nháp phiếu {receipt.MaPhieu}.",
                RedirectUrl = Url.Action(nameof(ReceiptDetails), new { id = receipt.Id })
            };
        }

        private async Task<List<FieldError>> ValidateReceiptAsync(GoodsReceiptViewModel model)
        {
            var errors = new List<FieldError>();
            var validLoaiNhap = new[] { "MuaNCC", "ChuyenKho", "HangTraVe", "VienTro" };
            var validKhoNhap = new[] { "KhoChan", "KhoLe", "NhaThuoc" };

            if (!validLoaiNhap.Contains(model.LoaiNhap))
                errors.Add(new FieldError("header.loaiNhap", "Loại nhập không hợp lệ."));

            if (!validKhoNhap.Contains(model.KhoNhap))
                errors.Add(new FieldError("header.khoNhap", "Kho nhập không hợp lệ."));

            if (model.LoaiNhap == "MuaNCC" && !model.NhaCungCapId.HasValue)
                errors.Add(new FieldError("header.nhaCungCapId", "Loại nhập \"Mua nhà cung cấp\" bắt buộc chọn nhà cung cấp."));

            if (model.NhaCungCapId.HasValue &&
                !await _context.Suppliers.AnyAsync(s => s.Id == model.NhaCungCapId.Value))
            {
                errors.Add(new FieldError("header.nhaCungCapId", "Nhà cung cấp không hợp lệ."));
            }

            if (model.ChiTiet == null || model.ChiTiet.Count == 0)
            {
                errors.Add(new FieldError("chiTiet", "Phiếu phải có ít nhất một dòng thuốc."));
                return errors;
            }

            var medicineIds = model.ChiTiet.Select(c => c.ThuocId).Distinct().ToList();
            var medicines = await _context.Medicines
                .Where(m => medicineIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            var canDateThreshold = DateTime.Today.AddMonths(CanDateThresholdMonths);

            for (var i = 0; i < model.ChiTiet.Count; i++)
            {
                var line = model.ChiTiet[i];
                var prefix = $"chiTiet.{i}.";

                if (!medicines.ContainsKey(line.ThuocId))
                {
                    errors.Add(new FieldError(prefix + "thuocId", "Vui lòng chọn thuốc hợp lệ."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.SoLo))
                    errors.Add(new FieldError(prefix + "soLo", "Số lô là bắt buộc."));

                if (!line.HanSuDung.HasValue)
                {
                    errors.Add(new FieldError(prefix + "hanSuDung", "Hạn sử dụng là bắt buộc."));
                }
                else if (line.HanSuDung.Value.Date <= DateTime.Today)
                {
                    errors.Add(new FieldError(prefix + "hanSuDung", "Hạn sử dụng phải ở trong tương lai."));
                }
                else if (line.HanSuDung.Value.Date < canDateThreshold && !line.XacNhanCanDate)
                {
                    errors.Add(new FieldError(prefix + "hanSuDung", "Thuốc cận date - cần xác nhận trước khi lưu."));
                }

                if (line.SoLuong <= 0)
                    errors.Add(new FieldError(prefix + "soLuong", "Số lượng phải là số nguyên dương."));

                if (line.DonGia < 0)
                    errors.Add(new FieldError(prefix + "donGia", "Đơn giá không được âm."));

                if (line.PhanTramVAT < 0 || line.PhanTramVAT > 100)
                    errors.Add(new FieldError(prefix + "phanTramVAT", "% VAT phải nằm trong khoảng 0-100."));
            }

            return errors;
        }

        private static void ApplyTotals(GoodsReceipt receipt, List<GoodsReceiptDetailViewModel> lines)
        {
            receipt.TongSoMatHang = lines.Count;
            receipt.TongTienTruocVAT = Math.Round(lines.Sum(l => l.SoLuong * l.DonGia), 2);
            receipt.TienVAT = Math.Round(lines.Sum(l => l.SoLuong * l.DonGia * (l.PhanTramVAT / 100m)), 2);
            receipt.TongThanhToan = receipt.TongTienTruocVAT + receipt.TienVAT;
        }

        private string GenerateNextMaPhieu(int year)
        {
            var prefix = $"PN-{year}-";
            var maxSeq = _context.GoodsReceipts
                .Where(r => r.MaPhieu.StartsWith(prefix))
                .Select(r => r.MaPhieu)
                .AsEnumerable()
                .Select(m => int.TryParse(m.Substring(prefix.Length), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            return $"{prefix}{maxSeq + 1:D4}";
        }

        // ================================================================
        // Quy trình duyệt
        // ================================================================

        // POST: Admin/Medicines/ApproveReceipt/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReceipt(int id)
        {
            if (!await CurrentUserCanApproveAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt phiếu nhập kho.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            var receipt = await _context.GoodsReceipts
                .Include(r => r.ChiTiet)
                .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (receipt == null) return NotFound();

            if (receipt.TrangThai != "ChoDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể duyệt phiếu đang ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var line in receipt.ChiTiet)
            {
                MedicineBatch? batch = null;
                if (line.CongDonVaoLoHienCo)
                {
                    // Đối chiếu lại lô hiện có tại đúng thời điểm duyệt (có thể
                    // đã thay đổi so với lúc nhập liệu), không tin vào lựa chọn
                    // cũ một cách mù quáng.
                    batch = await _context.MedicineBatches
                        .FirstOrDefaultAsync(b => b.ThuocId == line.ThuocId && b.SoLo == line.SoLo);
                }

                if (batch != null)
                {
                    batch.SoLuongNhap += line.SoLuong;
                    batch.SoLuongTon += line.SoLuong;
                    batch.GiaNhap = line.DonGia;
                    batch.NhaCungCapId = receipt.NhaCungCapId;
                    _context.Entry(batch).State = EntityState.Modified;
                }
                else
                {
                    batch = new MedicineBatch
                    {
                        ThuocId = line.ThuocId,
                        SoLo = line.SoLo,
                        NgayNhap = receipt.NgayNhap,
                        HanSuDung = line.HanSuDung,
                        SoLuongNhap = line.SoLuong,
                        SoLuongTon = line.SoLuong,
                        GiaNhap = line.DonGia,
                        NhaCungCapId = receipt.NhaCungCapId
                    };
                    _context.MedicineBatches.Add(batch);
                    await _context.SaveChangesAsync(); // sinh batch.Id để gắn vào chi tiết phiếu
                }

                line.LoThuocId = batch.Id;
                _context.Entry(line).State = EntityState.Modified;

                line.Medicine.TonKho += line.SoLuong;
                _context.Entry(line.Medicine).State = EntityState.Modified;
            }

            receipt.TrangThai = "DaDuyet";
            receipt.NguoiDuyetId = GetCurrentUserId();
            receipt.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Duyệt phiếu nhập kho",
                ChiTiet = $"Duyệt phiếu {receipt.MaPhieu}, tổng thanh toán {receipt.TongThanhToan:N0}đ, {receipt.ChiTiet.Count} dòng thuốc - đã cộng vào tồn kho tương ứng.",
                DoiTuongLoai = "PhieuNhapKho",
                DoiTuongId = receipt.Id
            });
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Đã duyệt phiếu {receipt.MaPhieu}, tồn kho đã được cập nhật.";
            return RedirectToAction(nameof(ReceiptDetails), new { id });
        }

        // POST: Admin/Medicines/RejectReceipt/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReceipt(int id, string lyDo)
        {
            if (!await CurrentUserCanApproveAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt/từ chối phiếu nhập kho.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            var receipt = await _context.GoodsReceipts.FirstOrDefaultAsync(r => r.Id == id);
            if (receipt == null) return NotFound();

            if (receipt.TrangThai != "ChoDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối phiếu đang ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            receipt.TrangThai = "TuChoi";
            receipt.LyDoTuChoi = lyDo.Trim();
            receipt.NguoiDuyetId = GetCurrentUserId();
            receipt.NgayDuyet = DateTime.Now;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Từ chối phiếu nhập kho",
                ChiTiet = $"Từ chối phiếu {receipt.MaPhieu}. Lý do: {receipt.LyDoTuChoi}",
                DoiTuongLoai = "PhieuNhapKho",
                DoiTuongId = receipt.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã từ chối phiếu {receipt.MaPhieu}.";
            return RedirectToAction(nameof(ReceiptDetails), new { id });
        }

        // POST: Admin/Medicines/CancelReceipt/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReceipt(int id)
        {
            var receipt = await _context.GoodsReceipts.FirstOrDefaultAsync(r => r.Id == id);
            if (receipt == null) return NotFound();

            if (receipt.TrangThai == "DaDuyet" || receipt.TrangThai == "DaHuy")
            {
                TempData["ErrorMessage"] = "Không thể hủy phiếu đã duyệt hoặc đã hủy.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            receipt.TrangThai = "DaHuy";

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Hủy phiếu nhập kho",
                ChiTiet = $"Hủy phiếu {receipt.MaPhieu}.",
                DoiTuongLoai = "PhieuNhapKho",
                DoiTuongId = receipt.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã hủy phiếu {receipt.MaPhieu}.";
            return RedirectToAction(nameof(Receipts));
        }

        // ================================================================
        // Danh sách & chi tiết phiếu
        // ================================================================

        // GET: Admin/Medicines/Receipts
        [HttpGet]
        public async Task<IActionResult> Receipts(string? trangThai)
        {
            var query = _context.GoodsReceipts
                .Include(r => r.NhaCungCap)
                .Include(r => r.NguoiTao)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(r => r.TrangThai == trangThai);

            var list = await query.OrderByDescending(r => r.NgayTao).ToListAsync();

            ViewBag.TrangThaiFilter = trangThai;
            ViewBag.CanApprove = await CurrentUserCanApproveAsync();
            return View(list);
        }

        // GET: Admin/Medicines/ReceiptDetails/5
        [HttpGet]
        public async Task<IActionResult> ReceiptDetails(int id)
        {
            var receipt = await LoadReceiptForDisplayAsync(id);
            if (receipt == null) return NotFound();

            ViewBag.CanApprove = await CurrentUserCanApproveAsync();
            return View(receipt);
        }

        // GET: Admin/Medicines/PrintReceipt/5  ("Phiếu nhập kho", in A4)
        [HttpGet]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var receipt = await LoadReceiptForDisplayAsync(id);
            if (receipt == null) return NotFound();

            if (receipt.TrangThai != "DaDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể in phiếu đã được duyệt.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            return View(receipt);
        }

        // GET: Admin/Medicines/PrintInspectionRecord/5  ("Biên bản kiểm nhập", in A4)
        [HttpGet]
        public async Task<IActionResult> PrintInspectionRecord(int id)
        {
            var receipt = await LoadReceiptForDisplayAsync(id);
            if (receipt == null) return NotFound();

            if (receipt.TrangThai != "DaDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể in biên bản cho phiếu đã được duyệt.";
                return RedirectToAction(nameof(ReceiptDetails), new { id });
            }

            return View(receipt);
        }

        private async Task<GoodsReceipt?> LoadReceiptForDisplayAsync(int id)
        {
            return await _context.GoodsReceipts
                .Include(r => r.NhaCungCap)
                .Include(r => r.NguoiTao)
                .Include(r => r.NguoiDuyet)
                .Include(r => r.PhieuGoc)
                .Include(r => r.ChiTiet).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // ================================================================
        // AJAX: combobox tìm thuốc + kiểm tra lô trùng
        // ================================================================

        // GET: Admin/Medicines/SearchMedicinesForReceipt?term=...
        [HttpGet]
        public async Task<IActionResult> SearchMedicinesForReceipt(string? term)
        {
            var query = _context.Medicines.AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(m => m.TenThuoc.Contains(term) || m.HoatChat.Contains(term));
            }

            var results = await query
                .OrderBy(m => m.TenThuoc)
                .Take(30)
                .Select(m => new
                {
                    id = m.Id,
                    text = m.TenThuoc + " (" + m.HoatChat + ")",
                    tenThuoc = m.TenThuoc,
                    hoatChat = m.HoatChat,
                    hamLuong = m.HamLuong,
                    quyCachDongGoi = m.QuyCachDongGoi,
                    donViTinh = m.DonViTinh,
                    tonKho = m.TonKho
                })
                .ToListAsync();

            return Json(new { results });
        }

        // GET: Admin/Medicines/CheckExistingLot?thuocId=5&soLo=B2024051
        [HttpGet]
        public async Task<IActionResult> CheckExistingLot(int thuocId, string? soLo)
        {
            if (string.IsNullOrWhiteSpace(soLo)) return Json(new { exists = false });

            var exists = await _context.MedicineBatches
                .AnyAsync(b => b.ThuocId == thuocId && b.SoLo == soLo.Trim());
            return Json(new { exists });
        }

        private async Task<bool> CurrentUserCanApproveAsync()
        {
            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            return currentUser?.DuocDuyetPhieuNhapKho == true;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
