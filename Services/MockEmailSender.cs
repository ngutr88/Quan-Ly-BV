using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuanLyBenhVien.Services
{
    // Chưa có nhà cung cấp email thật (SMTP/SendGrid...) - ghi log ứng dụng để
    // xác nhận luồng gọi đúng chỗ. Đây KHÔNG phải Nhật ký hệ thống (AuditLog) -
    // nội dung gửi (có thể chứa OTP/token) không bao giờ được đưa vào AuditLog.
    public class MockEmailSender : IEmailSender
    {
        private readonly ILogger<MockEmailSender> _logger;

        public MockEmailSender(ILogger<MockEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body)
        {
            _logger.LogInformation("[MockEmailSender] Gửi tới {To} - Tiêu đề: {Subject}", to, subject);
            return Task.CompletedTask;
        }
    }
}
