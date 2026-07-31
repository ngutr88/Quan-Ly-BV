using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Services
{
    public class SlotValidationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static SlotValidationResult Ok() => new() { Success = true };
        public static SlotValidationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Shared slot-availability logic for booking a new appointment
    /// (BookController.ConfirmBooking) and rescheduling an existing one
    /// (Patient/AppointmentsController.Reschedule) - a single source of truth
    /// instead of two copies of the same schedule/capacity/conflict checks.
    /// </summary>
    public class AppointmentSlotService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentSlotService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Every free "HH:mm" slot for a doctor on a given date, honoring their
        /// recurring work schedule and current booking load.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetAvailableSlotsAsync(int doctorId, DateTime date, int? excludingAppointmentId = null)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // SQLite cannot reliably translate ordering by TimeSpan. Materialize the
            // filtered rows first, then sort in memory to keep this endpoint responsive.
            var schedules = (await _context.DoctorWorkSchedules
                .AsNoTracking()
                .Where(s => s.BacSiId == doctorId &&
                            s.ThuTrongTuan == dayOfWeek &&
                            s.DangHoatDong &&
                            (s.HieuLucTu == null || s.HieuLucTu.Value.Date <= date.Date) &&
                            (s.HieuLucDen == null || s.HieuLucDen.Value.Date >= date.Date))
                .ToListAsync())
                .OrderBy(s => s.GioBatDau)
                .ToList();

            if (!schedules.Any())
            {
                return Array.Empty<string>();
            }

            var bookedQuery = _context.Appointments
                .Where(a => a.BacSiId == doctorId && a.ThoiGian.Date == date.Date && a.TrangThai != "DaHuy");
            if (excludingAppointmentId.HasValue)
            {
                bookedQuery = bookedQuery.Where(a => a.Id != excludingAppointmentId.Value);
            }

            // Đếm số bệnh nhân đã đặt theo từng khung giờ (không chỉ có/không) để so
            // sánh với SoBenhNhanToiDa của ca làm việc - một khung giờ có thể nhận
            // nhiều bệnh nhân nếu ca cho phép.
            var bookedCounts = (await bookedQuery
                .Select(a => a.ThoiGian.TimeOfDay)
                .ToListAsync())
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());

            var now = DateTime.Now;
            var slots = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var schedule in schedules)
            {
                var duration = TimeSpan.FromMinutes(schedule.ThoiLuongKhamPhut);
                for (var slot = schedule.GioBatDau; slot + duration <= schedule.GioKetThuc; slot += duration)
                {
                    var slotDateTime = date.Date.Add(slot);
                    var bookedCount = bookedCounts.GetValueOrDefault(slot);
                    if (slotDateTime <= now || bookedCount >= schedule.SoBenhNhanToiDa)
                    {
                        continue;
                    }

                    slots.Add(slot.ToString(@"hh\:mm"));
                }
            }

            return slots.ToList();
        }

        /// <summary>
        /// Re-validates a specific slot inside a transaction, right before
        /// claiming it - reduces the race window between two near-simultaneous
        /// bookings/reschedules. Pass <paramref name="excludingAppointmentId"/>
        /// when rescheduling so the appointment's own current row doesn't count
        /// against itself in the capacity/conflict checks.
        /// </summary>
        public async Task<SlotValidationResult> ValidateSlotAsync(int doctorId, DateTime appointmentTime, int patientId, int? excludingAppointmentId = null)
        {
            var dayOfWeek = (int)appointmentTime.DayOfWeek;
            var slotSchedule = await _context.DoctorWorkSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BacSiId == doctorId &&
                    s.ThuTrongTuan == dayOfWeek &&
                    s.DangHoatDong &&
                    s.GioBatDau <= appointmentTime.TimeOfDay && appointmentTime.TimeOfDay < s.GioKetThuc &&
                    (s.HieuLucTu == null || s.HieuLucTu.Value.Date <= appointmentTime.Date) &&
                    (s.HieuLucDen == null || s.HieuLucDen.Value.Date >= appointmentTime.Date));

            if (slotSchedule == null)
            {
                return SlotValidationResult.Fail("Bác sĩ không có ca làm việc phù hợp với khung giờ này. Vui lòng chọn lại.");
            }

            var doctorSlotQuery = _context.Appointments
                .Where(a => a.BacSiId == doctorId && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");
            if (excludingAppointmentId.HasValue)
            {
                doctorSlotQuery = doctorSlotQuery.Where(a => a.Id != excludingAppointmentId.Value);
            }

            var doctorSlotCount = await doctorSlotQuery.CountAsync();
            if (doctorSlotCount >= slotSchedule.SoBenhNhanToiDa)
            {
                return SlotValidationResult.Fail("Khung giờ này đã đủ số bệnh nhân tối đa. Vui lòng chọn khung giờ khác.");
            }

            var patientSlotQuery = _context.Appointments
                .Where(a => a.BenhNhanId == patientId && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");
            if (excludingAppointmentId.HasValue)
            {
                patientSlotQuery = patientSlotQuery.Where(a => a.Id != excludingAppointmentId.Value);
            }

            var patientSlotTaken = await patientSlotQuery.AnyAsync();
            if (patientSlotTaken)
            {
                return SlotValidationResult.Fail("Tài khoản của bạn đã có lịch khám trong cùng khung giờ này.");
            }

            return SlotValidationResult.Ok();
        }
    }
}
