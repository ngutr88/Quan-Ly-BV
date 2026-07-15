using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Helpers
{
    public static class DoctorScheduleHelper
    {
        private static readonly Regex TimeRangeRegex = new(
            @"(?<start>\d{1,2}:\d{2})\s*-\s*(?<end>\d{1,2}:\d{2})",
            RegexOptions.Compiled);

        private static readonly Regex WeekdayRegex = new(
            @"Thứ\s*(?<day>[2-7])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IReadOnlyList<DoctorWorkSchedule> BuildSchedulesFromDescription(int doctorId, string? description)
        {
            var normalized = description ?? string.Empty;
            var days = ExtractDays(normalized);
            var ranges = ExtractTimeRanges(normalized);

            return days
                .SelectMany(day => ranges.Select(range => new DoctorWorkSchedule
                {
                    BacSiId = doctorId,
                    ThuTrongTuan = day,
                    GioBatDau = range.Start,
                    GioKetThuc = range.End,
                    ThoiLuongKhamPhut = 30,
                    SoBenhNhanToiDa = 1,
                    DangHoatDong = true
                }))
                .Where(s => s.GioBatDau < s.GioKetThuc)
                .ToList();
        }

        private static IReadOnlyList<int> ExtractDays(string description)
        {
            if (description.Contains("24h", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { 0, 1, 2, 3, 4, 5, 6 };
            }

            if (description.Contains("Thứ 2 đến Thứ 7", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { 1, 2, 3, 4, 5, 6 };
            }

            if (description.Contains("Thứ 2 đến Thứ 6", StringComparison.OrdinalIgnoreCase) ||
                description.Contains("các ngày trong tuần", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { 1, 2, 3, 4, 5 };
            }

            var parsedDays = WeekdayRegex.Matches(description)
                .Select(match => int.Parse(match.Groups["day"].Value))
                .Select(MapVietnameseWeekday)
                .Distinct()
                .OrderBy(day => day)
                .ToList();

            return parsedDays.Any()
                ? parsedDays
                : new[] { 1, 2, 3, 4, 5 };
        }

        private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> ExtractTimeRanges(string description)
        {
            if (description.Contains("24h", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { (TimeSpan.Zero, new TimeSpan(23, 59, 0)) };
            }

            var ranges = TimeRangeRegex.Matches(description)
                .Select(match => (
                    Start: TimeSpan.Parse(match.Groups["start"].Value),
                    End: TimeSpan.Parse(match.Groups["end"].Value)))
                .Distinct()
                .ToList();

            return ranges.Any()
                ? ranges
                : new[]
                {
                    (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
                    (new TimeSpan(13, 30, 0), new TimeSpan(17, 30, 0))
                };
        }

        private static int MapVietnameseWeekday(int day)
        {
            return day == 7 ? 6 : day - 1;
        }
    }
}
