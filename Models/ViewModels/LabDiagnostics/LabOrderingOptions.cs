using System.Collections.Generic;

namespace QuanLyBenhVien.Models.ViewModels.LabDiagnostics
{
    // Danh mục dịch vụ CLS rút gọn cho màn chỉ định trong phiên khám (ExamController.Session).
    public class LabServiceOptionViewModel
    {
        public int Id { get; set; }
        public string NhomCLS { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public decimal Gia { get; set; }
    }

    public class LabBundleOptionViewModel
    {
        public int Id { get; set; }
        public string TenBo { get; set; } = string.Empty;
        public List<int> DichVuIds { get; set; } = new();
    }
}
