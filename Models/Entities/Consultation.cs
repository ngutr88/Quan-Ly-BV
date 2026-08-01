using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Mô hình B (chỉ nhắn thẳng bác sĩ điều trị, không sàng lọc, không chuyển
    // giao) => mỗi cặp (BenhNhan, BacSi) chỉ có đúng MỘT hội thoại tồn tại suốt
    // vòng đời (unique index bên dưới), được "mở lại" bằng cách đổi TrangThai
    // chứ không tạo hội thoại mới mỗi lần nhắn lại sau khi đã đóng.
    [Table("HoiThoaiTuVan")]
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "Moi"; // Moi, DangXuLy, DaTraLoi, DaDong

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Mốc "bắt đầu chờ trả lời" - null khi tin nhắn mới nhất là của bác sĩ
        // (hoặc hội thoại chưa có tin nào). KHÔNG reset mỗi khi bệnh nhân gửi
        // thêm tin trong cùng một đợt chờ, để "chờ N giờ" phản ánh đúng SLA
        // thay vì bị dồn tin làm trẻ hoá.
        public DateTime? ThoiGianChoTraLoiTu { get; set; }

        // Mốc tin nhắn gần nhất (bất kỳ ai gửi) - dùng để sắp xếp tab "Đã đóng"/
        // "Tất cả" theo hoạt động gần đây, khác ThoiGianChoTraLoiTu (chỉ có ý
        // nghĩa cho 2 tab "cần xử lý").
        public DateTime? ThoiGianTinNhanCuoi { get; set; }

        [StringLength(1000)]
        public string? GhiChuKetLuan { get; set; }

        public DateTime? NgayDong { get; set; }

        // Đảm bảo auto-reply ngoài giờ chỉ gửi ĐÚNG MỘT LẦN trong suốt vòng đời
        // hội thoại, không phải một lần mỗi đợt ngoài giờ.
        public bool DaGuiAutoReplyNgoaiGio { get; set; } = false;

        public virtual ICollection<ConversationMessage> TinNhan { get; set; } = new List<ConversationMessage>();
    }

    [Table("TinNhanTuVan")]
    public class ConversationMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HoiThoaiId { get; set; }

        [ForeignKey("HoiThoaiId")]
        public virtual Conversation HoiThoai { get; set; } = null!;

        // Null khi VaiTroNguoiGui = "HeThong" (tin auto-reply không gắn với một
        // tài khoản NguoiDung cụ thể nào).
        public int? NguoiGuiId { get; set; }

        [ForeignKey("NguoiGuiId")]
        public virtual User? NguoiGui { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTroNguoiGui { get; set; } = string.Empty; // Doctor, Patient, HeThong

        [Required]
        [StringLength(20)]
        public string Loai { get; set; } = "Text"; // Text, MoiDatLich, TuDongPhanHoi

        // Nullable + không [Required]: tin chỉ có ảnh (không chữ) vẫn hợp lệ -
        // ràng buộc "phải có nội dung HOẶC ảnh" nằm ở tầng controller.
        [StringLength(2000)]
        public string? NoiDung { get; set; }

        public DateTime ThoiGianGui { get; set; } = DateTime.Now;

        // Chat 1-1 nên luôn đúng 1 người nhận cho mỗi tin - không cần bảng
        // riêng theo dõi nhiều người xem, theo đúng pattern LabResult.DaXem.
        public bool DaXemBoiNguoiNhan { get; set; } = false;

        public DateTime? NgayXem { get; set; }

        public virtual ICollection<ConversationMessageAttachment> TepDinhKem { get; set; } = new List<ConversationMessageAttachment>();
    }

    // Theo đúng khuôn LabResultFile - cùng cách lưu file cục bộ ngoài wwwroot,
    // cùng shape cột.
    [Table("TepDinhKemTinNhan")]
    public class ConversationMessageAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TinNhanId { get; set; }

        [ForeignKey("TinNhanId")]
        public virtual ConversationMessage TinNhan { get; set; } = null!;

        [Required]
        [StringLength(260)]
        public string TenGoc { get; set; } = string.Empty;

        [Required]
        [StringLength(260)]
        public string TenLuuTru { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long KichThuoc { get; set; }

        public int ThuTu { get; set; }

        public DateTime NgayTaiLen { get; set; } = DateTime.Now;
    }
}
