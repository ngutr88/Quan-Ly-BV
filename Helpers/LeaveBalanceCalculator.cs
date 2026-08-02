using System;

namespace QuanLyBenhVien.Helpers
{
    // Toàn bộ hàm thuần (không đụng DB) cho công thức số dư phép năm - tách
    // riêng khỏi LeaveRequestService để unit-test được không cần EF/DB thật,
    // đúng khuôn TotpHelper/PasswordPolicyHelper.
    public static class LeaveBalanceCalculator
    {
        private const int BaseAnnualDays = 12;
        private const int YearsPerBonusDay = 5;

        // 12 ngày cơ bản + 1 ngày mỗi 5 năm kinh nghiệm (chia nguyên, không làm tròn lên)
        public static decimal ComputeAnnualQuota(int soNamKinhNghiem)
        {
            var yearsCounted = Math.Max(0, soNamKinhNghiem);
            return BaseAnnualDays + (yearsCounted / YearsPerBonusDay);
        }

        // Cộng dồn từ số dư còn lại của năm trước, giới hạn trần (mặc định 5 ngày)
        public static decimal ComputeCarryOver(decimal previousYearRemaining, decimal cap = 5m)
        {
            return Math.Clamp(previousYearRemaining, 0m, cap);
        }

        // Số ngày phép bị trừ cho 1 yêu cầu: số ngày lịch trong khoảng (bao gồm
        // 2 đầu mút), trừ 0.5 nếu chỉ xin nghỉ 1 buổi (chỉ hợp lệ khi tuNgay == denNgay).
        public static decimal ComputeRequestedDays(DateTime tuNgay, DateTime denNgay, string? buoi)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date;
            if (den < tu)
            {
                throw new ArgumentException("Đến ngày phải lớn hơn hoặc bằng Từ ngày.");
            }

            var calendarDays = (decimal)(den - tu).Days + 1m;

            if (!string.IsNullOrEmpty(buoi))
            {
                if (tu != den)
                {
                    throw new ArgumentException("Chỉ được chọn Buổi khi Từ ngày và Đến ngày trùng nhau.");
                }
                return 0.5m;
            }

            return calendarDays;
        }

        // Số ngày còn dùng được = tổng quota + cộng dồn - đã dùng (đã duyệt) - đang tạm giữ (chờ duyệt)
        public static decimal ComputeRemaining(decimal tongSoNgay, decimal congDonTuNamTruoc, decimal daDung, decimal daTamGiu)
        {
            return tongSoNgay + congDonTuNamTruoc - daDung - daTamGiu;
        }
    }
}
