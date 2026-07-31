using System.Threading.Tasks;

namespace QuanLyBenhVien.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }
}
