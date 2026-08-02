using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Services
{
    public class LeaveActionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Warning { get; set; }
        public LeaveRequest? Request { get; set; }

        public static LeaveActionResult Ok(LeaveRequest? request = null, string? warning = null) =>
            new() { Success = true, Request = request, Warning = warning };

        public static LeaveActionResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    // Toàn bộ logic nghiệp vụ đăng ký/duyệt/từ chối nghỉ phép - dùng chung cho
    // Areas/Doctor/Controllers/ScheduleController (Trưởng khoa duyệt) và
    // Areas/Admin/Controllers/LeaveApprovalsController (Admin duyệt) để không
    // lặp lại cùng 1 luồng trừ/hoàn số dư ở 2 nơi, cùng khuôn
    // AppointmentSlotService (DI Scoped, constructor nhận ApplicationDbContext).
    public class LeaveRequestService
    {
        private static readonly string[] ValidLoaiNghi = { "PhepNam", "Om", "ViecRieng", "Khac" };
        private const string TruongKhoa = "Trưởng khoa";

        private readonly ApplicationDbContext _context;

        public LeaveRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy dòng số dư phép của 1 bác sĩ cho 1 năm, tạo lười nếu chưa có -
        // backfill cộng dồn (giới hạn trần) từ số dư còn lại của năm liền trước
        // nếu dòng năm đó đã tồn tại.
        public async Task<LeaveBalance> GetOrCreateBalanceAsync(int doctorId, int year)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.BacSiId == doctorId && b.Nam == year);
            if (balance != null) return balance;

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) throw new InvalidOperationException("Không tìm thấy bác sĩ.");

            var quota = LeaveBalanceCalculator.ComputeAnnualQuota(doctor.SoNamKinhNghiem);

            var previousYear = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.BacSiId == doctorId && b.Nam == year - 1);
            var carryOver = 0m;
            if (previousYear != null)
            {
                var previousRemaining = LeaveBalanceCalculator.ComputeRemaining(
                    previousYear.TongSoNgay, previousYear.CongDonTuNamTruoc, previousYear.DaDung, previousYear.DaTamGiu);
                carryOver = LeaveBalanceCalculator.ComputeCarryOver(previousRemaining);
            }

            balance = new LeaveBalance
            {
                BacSiId = doctorId,
                Nam = year,
                TongSoNgay = quota,
                CongDonTuNamTruoc = carryOver,
                DaDung = 0,
                DaTamGiu = 0
            };
            _context.LeaveBalances.Add(balance);
            await _context.SaveChangesAsync();
            return balance;
        }

        public async Task<bool> HasScheduleConflictAsync(int doctorId, DateTime tuNgay, DateTime denNgay)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date;
            return await _context.Appointments.AnyAsync(a =>
                a.BacSiId == doctorId && a.TrangThai != "DaHuy" &&
                a.ThoiGian.Date >= tu && a.ThoiGian.Date <= den);
        }

        // Bác sĩ Trưởng khoa hiện tại của 1 khoa (nếu có), loại trừ 1 bác sĩ cụ
        // thể nếu cần (vd không tự thông báo cho chính người đang xin nghỉ).
        public async Task<Doctor?> GetHeadOfDepartmentAsync(int khoaId, int? excludeDoctorId = null)
        {
            return await _context.Doctors.FirstOrDefaultAsync(d =>
                d.KhoaId == khoaId && d.ChucVu == TruongKhoa &&
                (excludeDoctorId == null || d.Id != excludeDoctorId));
        }

        public async Task<LeaveActionResult> SubmitAsync(
            int doctorId, DateTime tuNgay, DateTime denNgay, string? buoi, string loaiNghi, string lyDo, string? dinhKemUrl)
        {
            if (!ValidLoaiNghi.Contains(loaiNghi))
            {
                return LeaveActionResult.Fail("Loại nghỉ không hợp lệ.");
            }
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return LeaveActionResult.Fail("Vui lòng nhập lý do nghỉ.");
            }

            var tu = tuNgay.Date;
            var den = denNgay.Date;
            if (den < tu)
            {
                return LeaveActionResult.Fail("Đến ngày phải lớn hơn hoặc bằng Từ ngày.");
            }
            if (tu.Year != den.Year)
            {
                return LeaveActionResult.Fail("Yêu cầu nghỉ không được vắt qua 2 năm - vui lòng tách thành 2 yêu cầu riêng theo từng năm.");
            }
            if (!string.IsNullOrEmpty(buoi) && buoi != "Sang" && buoi != "Chieu")
            {
                return LeaveActionResult.Fail("Buổi không hợp lệ.");
            }
            if (!string.IsNullOrEmpty(buoi) && tu != den)
            {
                return LeaveActionResult.Fail("Chỉ được chọn Buổi khi Từ ngày và Đến ngày trùng nhau.");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) return LeaveActionResult.Fail("Không tìm thấy bác sĩ.");

            var isSickLeave = loaiNghi == "Om";
            var requestedDays = LeaveBalanceCalculator.ComputeRequestedDays(tu, den, buoi);
            var soNgayTru = isSickLeave ? 0m : requestedDays;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            LeaveBalance? balance = null;
            if (!isSickLeave)
            {
                balance = await GetOrCreateBalanceAsync(doctorId, tu.Year);
                var remaining = LeaveBalanceCalculator.ComputeRemaining(
                    balance.TongSoNgay, balance.CongDonTuNamTruoc, balance.DaDung, balance.DaTamGiu);
                if (soNgayTru > remaining)
                {
                    return LeaveActionResult.Fail(
                        $"Yêu cầu {soNgayTru:0.#} ngày vượt quá số dư phép năm còn lại ({remaining:0.#} ngày). Vui lòng chọn khoảng ngày ngắn hơn.");
                }
            }

            var request = new LeaveRequest
            {
                BacSiId = doctorId,
                TuNgay = tu,
                DenNgay = den,
                Buoi = string.IsNullOrEmpty(buoi) ? null : buoi,
                SoNgayTru = soNgayTru,
                LoaiNghi = loaiNghi,
                LyDo = lyDo.Trim(),
                DinhKemUrl = dinhKemUrl,
                TrangThai = "ChoDuyet"
            };
            _context.LeaveRequests.Add(request);

            if (balance != null)
            {
                balance.DaTamGiu += soNgayTru;
                balance.NgayCapNhat = DateTime.Now;
            }

            var hasConflict = await HasScheduleConflictAsync(doctorId, tu, den);

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = doctor.NguoiDungId,
                HanhDong = "Gửi yêu cầu nghỉ phép",
                ChiTiet = $"Gửi yêu cầu #{request.Id} nghỉ {LoaiNghiLabel(loaiNghi)} từ {tu:dd/MM/yyyy} đến {den:dd/MM/yyyy}" +
                          (hasConflict ? " (trùng lịch khám đã có)." : "."),
                DoiTuongLoai = "YeuCauNghiPhep",
                DoiTuongId = request.Id
            });

            var head = await GetHeadOfDepartmentAsync(doctor.KhoaId, excludeDoctorId: doctor.Id);
            if (head != null)
            {
                var doctorName = await _context.Users.Where(u => u.Id == doctor.NguoiDungId).Select(u => u.HoTen).FirstOrDefaultAsync();
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = head.NguoiDungId,
                    NoiDung = $"[NghiPhep] Yêu cầu nghỉ phép mới cần duyệt|Bác sĩ {doctorName} gửi yêu cầu nghỉ {LoaiNghiLabel(loaiNghi)} từ {tu:dd/MM/yyyy} đến {den:dd/MM/yyyy}."
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var warning = hasConflict
                ? "Lưu ý: khoảng ngày này đang trùng với lịch khám đã có của bạn - Trưởng khoa sẽ thấy cảnh báo này khi duyệt."
                : null;
            return LeaveActionResult.Ok(request, warning);
        }

        public async Task<LeaveActionResult> ApproveAsync(int requestId, int approverUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var request = await _context.LeaveRequests
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return LeaveActionResult.Fail("Không tìm thấy yêu cầu.");
            if (request.TrangThai != "ChoDuyet")
            {
                return LeaveActionResult.Fail("Chỉ có thể duyệt yêu cầu đang ở trạng thái Chờ duyệt.");
            }

            if (request.LoaiNghi != "Om" && request.SoNgayTru > 0)
            {
                var balance = await GetOrCreateBalanceAsync(request.BacSiId, request.TuNgay.Year);
                balance.DaTamGiu = Math.Max(0, balance.DaTamGiu - request.SoNgayTru);
                balance.DaDung += request.SoNgayTru;
                balance.NgayCapNhat = DateTime.Now;
            }

            request.TrangThai = "DaDuyet";
            request.NguoiDuyetId = approverUserId;
            request.NgayDuyet = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = request.Doctor.NguoiDungId,
                NoiDung = $"[NghiPhep] Yêu cầu nghỉ phép đã được duyệt|Yêu cầu nghỉ {LoaiNghiLabel(request.LoaiNghi)} từ {request.TuNgay:dd/MM/yyyy} đến {request.DenNgay:dd/MM/yyyy} của bạn đã được duyệt."
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = approverUserId,
                HanhDong = "Duyệt yêu cầu nghỉ phép",
                ChiTiet = $"Duyệt yêu cầu #{request.Id} của {request.Doctor.NguoiDungId} (nghỉ {LoaiNghiLabel(request.LoaiNghi)} {request.TuNgay:dd/MM/yyyy}-{request.DenNgay:dd/MM/yyyy}).",
                DoiTuongLoai = "YeuCauNghiPhep",
                DoiTuongId = request.Id
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return LeaveActionResult.Ok(request);
        }

        public async Task<LeaveActionResult> RejectAsync(int requestId, int approverUserId, string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return LeaveActionResult.Fail("Vui lòng nhập lý do từ chối.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var request = await _context.LeaveRequests
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return LeaveActionResult.Fail("Không tìm thấy yêu cầu.");
            if (request.TrangThai != "ChoDuyet")
            {
                return LeaveActionResult.Fail("Chỉ có thể từ chối yêu cầu đang ở trạng thái Chờ duyệt.");
            }

            if (request.LoaiNghi != "Om" && request.SoNgayTru > 0)
            {
                var balance = await GetOrCreateBalanceAsync(request.BacSiId, request.TuNgay.Year);
                balance.DaTamGiu = Math.Max(0, balance.DaTamGiu - request.SoNgayTru);
                balance.NgayCapNhat = DateTime.Now;
            }

            request.TrangThai = "TuChoi";
            request.LyDoTuChoi = lyDo.Trim();
            request.NguoiDuyetId = approverUserId;
            request.NgayDuyet = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = request.Doctor.NguoiDungId,
                NoiDung = $"[NghiPhep] Yêu cầu nghỉ phép bị từ chối|Yêu cầu nghỉ {LoaiNghiLabel(request.LoaiNghi)} từ {request.TuNgay:dd/MM/yyyy} đến {request.DenNgay:dd/MM/yyyy} bị từ chối. Lý do: {request.LyDoTuChoi}"
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = approverUserId,
                HanhDong = "Từ chối yêu cầu nghỉ phép",
                ChiTiet = $"Từ chối yêu cầu #{request.Id}. Lý do: {request.LyDoTuChoi}",
                DoiTuongLoai = "YeuCauNghiPhep",
                DoiTuongId = request.Id
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return LeaveActionResult.Ok(request);
        }

        public static string LoaiNghiLabel(string loaiNghi) => loaiNghi switch
        {
            "PhepNam" => "phép năm",
            "Om" => "ốm",
            "ViecRieng" => "việc riêng",
            "Khac" => "khác",
            _ => loaiNghi
        };
    }
}
