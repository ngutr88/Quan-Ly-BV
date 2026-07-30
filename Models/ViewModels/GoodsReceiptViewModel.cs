namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Header + chi tiết của một phiếu nhập kho thuốc, dùng chung cho form tạo
/// mới và chỉnh sửa (Nháp/Từ chối). Binding từ FormData qua action POST
/// (không phải JSON) để giữ nguyên cơ chế antiforgery token hiện có của dự án.
/// </summary>
public class GoodsReceiptViewModel
{
    public int? Id { get; set; }

    public int? PhieuGocId { get; set; }

    public string MaPhieu { get; set; } = string.Empty;

    public DateTime NgayNhap { get; set; } = DateTime.Now;

    public string LoaiNhap { get; set; } = "MuaNCC";

    public int? NhaCungCapId { get; set; }

    public string? SoHoaDonNCC { get; set; }

    public DateTime? NgayHoaDon { get; set; }

    public string KhoNhap { get; set; } = "KhoChan";

    public string? NguoiGiaoHang { get; set; }

    public string? GhiChu { get; set; }

    public string TrangThai { get; set; } = "Nhap";

    public List<GoodsReceiptDetailViewModel> ChiTiet { get; set; } = new();
}

public class GoodsReceiptDetailViewModel
{
    public int? Id { get; set; }

    public int ThuocId { get; set; }

    public string SoLo { get; set; } = string.Empty;

    public DateTime? HanSuDung { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal PhanTramVAT { get; set; }

    public bool XacNhanCanDate { get; set; }

    public bool CongDonVaoLoHienCo { get; set; }
}

/// <summary>Kết quả JSON trả về cho các thao tác AJAX (lưu nháp/gửi duyệt...).</summary>
public class GoodsReceiptActionResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public int? Id { get; set; }

    public string? MaPhieu { get; set; }

    public string? RedirectUrl { get; set; }

    /// <summary>Lỗi theo từng ô, key dạng "header.ngayNhap" hoặc "chiTiet.2.soLuong"
    /// để JS phía client biết chính xác input nào cần bôi đỏ.</summary>
    public List<FieldError> Errors { get; set; } = new();
}

/// <summary>Liên kết ngược từ một lô thuốc (LoThuoc) tới phiếu nhập kho đã
/// tạo ra nó, dùng để hiển thị trên trang danh sách lô thuốc.</summary>
public class BatchReceiptLink
{
    public int PhieuNhapKhoId { get; set; }

    public string MaPhieu { get; set; } = string.Empty;
}

public class FieldError
{
    public FieldError(string field, string message)
    {
        Field = field;
        Message = message;
    }

    public string Field { get; set; }

    public string Message { get; set; }
}
