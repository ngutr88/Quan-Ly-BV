using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers
{
    // Tên khác "SecurityStampValidator" để không trùng khái niệm với lớp cùng
    // tên của Microsoft.AspNetCore.Identity - dự án này không dùng Identity,
    // đây là cơ chế thu hồi phiên tự xây tương tự. Chạy mỗi request (không có
    // ValidationInterval) vì mục đích là đóng NGAY cửa sổ "phiên cũ còn dùng
    // được sau khi đổi mật khẩu" - đặt trễ sẽ làm mất tác dụng của tính năng.
    public static class SecurityStampCookieValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
            var stampClaim = context.Principal?.FindFirst("SecurityStamp");
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var currentStamp = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (string?)u.SecurityStamp)
                .FirstOrDefaultAsync();

            if (currentStamp == null || stampClaim == null || currentStamp != stampClaim.Value)
            {
                context.RejectPrincipal();
                return;
            }

            // Lớp thứ 2, song song SecurityStamp toàn cục ở trên: claim "sid"
            // trỏ tới ĐÚNG 1 dòng PhienDangNhap, cho phép thu hồi 1 phiên
            // riêng lẻ (Đăng xuất 1 thiết bị) mà không phải bump SecurityStamp
            // và đá hết mọi phiên khác. Không phải mọi cookie đều có "sid"
            // (phiên tạo trước Sprint 3) - coi như hợp lệ nếu thiếu claim này.
            var sidClaim = context.Principal?.FindFirst("sid");
            if (sidClaim != null)
            {
                var session = await db.LoginSessions
                    .FirstOrDefaultAsync(s => s.SessionToken == sidClaim.Value && s.NguoiDungId == userId);

                if (session == null || session.TrangThai != "HoatDong")
                {
                    context.RejectPrincipal();
                    return;
                }

                // Throttle ghi DB - chỉ cập nhật "hoạt động gần nhất" khi đã
                // cũ quá 5 phút, tránh ghi mỗi request (validator này chạy
                // trên MỌI request đã đăng nhập).
                if ((DateTime.Now - session.ThoiGianHoatDongCuoi).TotalMinutes >= 5)
                {
                    session.ThoiGianHoatDongCuoi = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
