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
            }
        }
    }
}
