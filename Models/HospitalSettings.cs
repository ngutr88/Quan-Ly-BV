namespace QuanLyBenhVien.Models
{
    public class HospitalSettings
    {
        public string TenBenhVien { get; set; } = "Bệnh viện Đa khoa Quốc tế MediFlow";
        public string Hotline { get; set; } = "1900 6868";
        public string HotlineCapCuu { get; set; } = "028.115";
        public string EmailHoTro { get; set; } = "support@mediflow.vn";
        public string DiaChi { get; set; } = "Số 120 Đường Ba Tháng Hai, Quận 10, TP. Hồ Chí Minh";
        public string GioLamViec { get; set; } = "Thứ Hai - Chủ Nhật: 07:30 - 21:00";
        public int ThoiGianKhamCa { get; set; } = 20; // Minutes per appointment
        public int SoBenhNhanToiDaMoiCa { get; set; } = 5; // Max patients per slot
        public int NguongCanhBaoTonKho { get; set; } = 50; // Threshold alert for low stock medicines
        public int ThueVat { get; set; } = 8; // VAT rate %
        public bool BatTatThongBao { get; set; } = true; // Switch to turn notifications on/off

        // Giờ hành chính riêng cho kênh Tin nhắn tư vấn - tách khỏi GioLamViec
        // (chuỗi tự do, không parse được) vì auto-reply ngoài giờ và dòng cam
        // kết phản hồi cần giờ/thứ có cấu trúc để so sánh được.
        public string TuVanGioBatDau { get; set; } = "07:30";
        public string TuVanGioKetThuc { get; set; } = "17:00";
        public System.DayOfWeek TuVanNgayApDungTu { get; set; } = System.DayOfWeek.Monday;
        public System.DayOfWeek TuVanNgayApDungDen { get; set; } = System.DayOfWeek.Saturday;
        public int TuVanCamKetPhanHoiTuGio { get; set; } = 24;
        public int TuVanCamKetPhanHoiDenGio { get; set; } = 48;
    }
}
