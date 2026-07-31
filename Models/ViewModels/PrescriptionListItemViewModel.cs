using System;

namespace QuanLyBenhVien.Models.ViewModels
{
    public class PrescriptionListItemViewModel
    {
        public int Id { get; set; }
        public DateTime NgayKe { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ChanDoan { get; set; } = string.Empty;
        public int SoLoaiThuoc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}
