using System;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single source of truth for "is this moment inside consultation-chat
/// business hours" - backs both the commitment banner on the patient side and
/// the after-hours auto-reply trigger, so they can never disagree about what
/// counts as "ngoài giờ".
/// </summary>
public static class ConsultationHoursHelper
{
    public static bool IsWithinBusinessHours(HospitalSettings settings, DateTime moment)
    {
        if (!InAppliedDayRange(settings, moment.DayOfWeek)) return false;

        if (!TimeSpan.TryParse(settings.TuVanGioBatDau, out var start) ||
            !TimeSpan.TryParse(settings.TuVanGioKetThuc, out var end))
        {
            return true; // cấu hình hỏng - không chặn nhắn tin vì lỗi cấu hình
        }

        var timeOfDay = moment.TimeOfDay;
        return timeOfDay >= start && timeOfDay < end;
    }

    // Hỗ trợ cả khoảng không vòng qua Chủ Nhật (Mon->Sat, mặc định) lẫn
    // khoảng có thể vòng tuần (vd Fri->Mon) nếu Admin cấu hình như vậy.
    private static bool InAppliedDayRange(HospitalSettings settings, DayOfWeek day)
    {
        var from = (int)settings.TuVanNgayApDungTu;
        var to = (int)settings.TuVanNgayApDungDen;
        var current = (int)day;

        return from <= to
            ? current >= from && current <= to
            : current >= from || current <= to;
    }
}
