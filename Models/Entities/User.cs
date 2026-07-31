using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("NguoiDung")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Sdt { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string MatKhauHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string VaiTro { get; set; } = string.Empty; // Admin, Doctor, Patient

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Active"; // Active, Blocked

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [StringLength(260)]
        public string? AnhDaiDien { get; set; }

        // Soft-delete: xóa tài khoản không được xóa cứng để tránh mất lịch sử
        // khám chữa bệnh liên đới (lịch khám, phiếu khám, đơn thuốc, hóa đơn...).
        public bool DaXoa { get; set; } = false;

        public DateTime? NgayXoa { get; set; }

        public int? XoaBoiId { get; set; }

        [ForeignKey("XoaBoiId")]
        public virtual User? XoaBoi { get; set; }

        // Cho phép tài khoản Admin này duyệt phiếu nhập kho thuốc (vai trò
        // nghiệp vụ "thủ kho"/"trưởng khoa dược"). Hệ thống hiện chỉ có 3 vai
        // trò (Admin/Doctor/Patient) nên đây là cờ phân quyền chi tiết cấp
        // tài khoản, không phải một VaiTro mới, để không phải mở rộng danh
        // sách vai trò/ràng buộc CHECK đã có.
        public bool DuocDuyetPhieuNhapKho { get; set; } = false;

        // Cho phép tài khoản Admin này xử lý phiếu chỉ định cận lâm sàng (vai
        // trò nghiệp vụ "kỹ thuật viên CLS") - cùng cách làm như
        // DuocDuyetPhieuNhapKho ở trên, không mở rộng danh sách VaiTro.
        public bool DuocXuLyCLS { get; set; } = false;

        // Đổi mỗi khi mật khẩu tài khoản này thay đổi (tự phục vụ hoặc do
        // Admin) - phiên đăng nhập cũ mang claim SecurityStamp cũ sẽ bị
        // SecurityStampCookieValidator từ chối ở lần request kế tiếp, tức
        // "thu hồi toàn bộ phiên" ngay lập tức. Default ngay trên property để
        // mọi nơi khởi tạo User (kể cả DbSeeder) tự có stamp riêng không cần sửa.
        [Required]
        [StringLength(64)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        // Bắt đổi mật khẩu ở lần đăng nhập kế tiếp - dùng cho mật khẩu tạm do
        // Admin cấp (StaffController.IssueTempPassword).
        public bool PhaiDoiMatKhau { get; set; } = false;

        // Hạn của mật khẩu tạm do Admin cấp (null nếu không phải mật khẩu tạm).
        // Cần cột riêng vì nhánh cấp mật khẩu tạm không tạo hàng
        // YeuCauKhoiPhucMatKhau nào để lưu hạn ở đó.
        public DateTime? MatKhauTamHetHan { get; set; }

        // Navigation properties
        public virtual Doctor? DoctorProfile { get; set; }
        public virtual Patient? PatientProfile { get; set; }
    }

    [Table("BacSi")]
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public int KhoaId { get; set; }

        [ForeignKey("KhoaId")]
        public virtual Department Department { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ChuyenKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string HocVi { get; set; } = string.Empty; // BS, ThS, TS, PGS, GS

        public int SoNamKinhNghiem { get; set; }

        [StringLength(200)]
        public string LichLamViec { get; set; } = "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)";

        [Required]
        [StringLength(50)]
        public string ChucVu { get; set; } = "Bác sĩ"; // Trưởng khoa, Phó trưởng khoa, Bác sĩ

        // Soft-delete: gỡ một bác sĩ khỏi hoạt động không được xóa cứng, để giữ
        // lại lịch sử LichKham/DanhGia đã gắn với BacSiId này.
        public bool DaXoa { get; set; } = false;

        public DateTime? NgayXoa { get; set; }

        public int? XoaBoiId { get; set; }

        [ForeignKey("XoaBoiId")]
        public virtual User? XoaBoi { get; set; }

        public virtual ICollection<DoctorWorkSchedule> WorkSchedules { get; set; } = new List<DoctorWorkSchedule>();
    }

    [Table("BenhNhan")]
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public DateTime NgaySinh { get; set; }

        [Required]
        [StringLength(10)]
        public string GioiTinh { get; set; } = string.Empty;

        [StringLength(10)]
        public string NhomMau { get; set; } = "O+";

        // Nullable: một số bệnh nhân chưa có BHYT. NULL nghĩa là "chưa có",
        // khác với chuỗi rỗng, để ràng buộc UNIQUE chỉ áp dụng khi có giá trị.
        [StringLength(50)]
        public string? SoBHYT { get; set; }

        public DateTime? NgayHetHanBHYT { get; set; }

        // Nullable: một số bệnh nhân (vd. trẻ em) chưa có CCCD. NULL nghĩa là
        // "chưa có", khác với chuỗi rỗng, để ràng buộc UNIQUE chỉ áp dụng khi có giá trị.
        [StringLength(20)]
        public string? SoCCCD { get; set; }

        [StringLength(500)]
        public string TienSuBenh { get; set; } = string.Empty;

        [StringLength(500)]
        public string DiUng { get; set; } = string.Empty;

        // Mức độ tổng quát của dị ứng đã khai báo ở trên - 1 trường duy nhất
        // tương xứng với cách DiUng đang được mô hình (1 chuỗi duy nhất, không
        // phải danh sách nhiều dị nguyên mỗi cái 1 mức độ riêng).
        [StringLength(20)]
        public string? MucDoDiUng { get; set; } // Nhe, TrungBinh, NghiemTrong

        // Soft-delete: gỡ một hồ sơ bệnh nhân không được xóa cứng, để giữ lại
        // toàn bộ lịch sử khám chữa bệnh (LichKham, PhieuKham, DonThuoc, HoaDon...).
        public bool DaXoa { get; set; } = false;

        public DateTime? NgayXoa { get; set; }

        public int? XoaBoiId { get; set; }

        [ForeignKey("XoaBoiId")]
        public virtual User? XoaBoi { get; set; }

        public virtual ICollection<Dependent> Dependents { get; set; } = new List<Dependent>();
    }
}
