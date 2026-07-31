using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Dữ liệu tương tác thuốc + nhóm chéo phản ứng dưới đây là DỮ LIỆU MINH HỌA
    // (LaDuLieuMinhHoa = true cho toàn bộ dữ liệu seed) - CHƯA qua thẩm định
    // dược lý lâm sàng, chỉ phục vụ minh họa cơ chế cảnh báo 3 mức. Không dùng
    // làm nguồn tra cứu y khoa chính thức. UI hiển thị cảnh báo phải luôn kèm
    // ghi chú này.
    [Table("TuongTacThuoc")]
    public class DrugInteraction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string HoatChatA { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string HoatChatB { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MucDoTuongTac { get; set; } = string.Empty; // ChongChiDinh, NghiemTrong, TrungBinh, Nhe

        [Required]
        [StringLength(500)]
        public string MoTa { get; set; } = string.Empty;

        [StringLength(30)]
        public string NguonNhap { get; set; } = "SeedDemo";

        public bool LaDuLieuMinhHoa { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    // Nhóm hoạt chất có nguy cơ dị ứng chéo (vd "Beta-lactam") - dị ứng khai
    // báo với 1 hoạt chất trong nhóm sẽ chặn cứng mọi hoạt chất khác cùng nhóm.
    [Table("NhomHoatChatCheoPhanUng")]
    public class DrugAllergyGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string TenNhom { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public bool LaDuLieuMinhHoa { get; set; } = true;

        public virtual ICollection<DrugAllergyGroupMember> ThanhVien { get; set; } = new List<DrugAllergyGroupMember>();
    }

    [Table("ThanhVienNhomHoatChat")]
    public class DrugAllergyGroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NhomId { get; set; }

        [ForeignKey("NhomId")]
        public virtual DrugAllergyGroup Nhom { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string HoatChat { get; set; } = string.Empty;
    }

    // Ghi lại đơn thuốc đã trừ kho từ (những) lô cụ thể nào - bắt buộc phải có
    // để "Sửa/Hủy đơn Chờ cấp phát" (Sprint 2) hoàn trả đúng lô thay vì chỉ
    // cộng bừa vào tổng Medicine.TonKho (sẽ làm sai lệch dữ liệu lô/hạn dùng).
    [Table("PhanBoLoThuocDonThuoc")]
    public class PrescriptionBatchAllocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChiTietDonThuocId { get; set; }

        [ForeignKey("ChiTietDonThuocId")]
        public virtual PrescriptionDetail ChiTietDonThuoc { get; set; } = null!;

        [Required]
        public int LoThuocId { get; set; }

        [ForeignKey("LoThuocId")]
        public virtual MedicineBatch LoThuoc { get; set; } = null!;

        [Required]
        public int SoLuongLay { get; set; }

        public bool DaHoanTra { get; set; } = false;

        public DateTime NgayPhanBo { get; set; } = DateTime.Now;
    }
}
