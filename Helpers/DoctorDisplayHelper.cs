using System;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Helpers
{
    public static class DoctorDisplayHelper
    {
        public static string FormatDoctorName(Doctor doctor)
        {
            var fullName = doctor.User?.HoTen?.Trim() ?? string.Empty;
            if (fullName.StartsWith("BS.", StringComparison.OrdinalIgnoreCase))
            {
                fullName = fullName[3..].TrimStart();
            }

            var degree = doctor.HocVi?.Trim().TrimEnd('.') ?? string.Empty;
            return string.IsNullOrEmpty(degree) ? fullName : $"{degree}. {fullName}";
        }
    }
}
