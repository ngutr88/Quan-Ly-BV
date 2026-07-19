using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    /// <summary>
    /// A news item or health notice published on the public website. Written by
    /// admin staff; only published rows are visible to anonymous visitors.
    /// </summary>
    [Table("TinTuc")]
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết.")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        public string TieuDe { get; set; } = string.Empty;

        /// <summary>Short summary used on cards and list pages.</summary>
        [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
        [StringLength(400, ErrorMessage = "Mô tả ngắn tối đa 400 ký tự.")]
        public string TomTat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết.")]
        public string NoiDung { get; set; } = string.Empty;

        /// <summary>Tin tức, Thông báo, Sức khỏe, Tuyển dụng.</summary>
        [Required]
        [StringLength(50)]
        public string ChuyenMuc { get; set; } = "Tin tức";

        [StringLength(300)]
        public string AnhBia { get; set; } = string.Empty;

        [StringLength(100)]
        public string TacGia { get; set; } = string.Empty;

        /// <summary>Drafts stay hidden from the public site until published.</summary>
        public bool DaXuatBan { get; set; } = true;

        /// <summary>Pinned articles lead the public news list.</summary>
        public bool NoiBat { get; set; }

        public int LuotXem { get; set; }

        public DateTime NgayDang { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }
    }
}
