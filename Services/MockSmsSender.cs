using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuanLyBenhVien.Services
{
    // Chưa có nhà cung cấp SMS thật - xem ghi chú ở MockEmailSender.
    public class MockSmsSender : ISmsSender
    {
        private readonly ILogger<MockSmsSender> _logger;

        public MockSmsSender(ILogger<MockSmsSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string message)
        {
            _logger.LogInformation("[MockSmsSender] Gửi SMS tới {To}", to);
            return Task.CompletedTask;
        }
    }
}
