using System.Threading.Tasks;

namespace QuanLyBenhVien.Services
{
    public interface ISmsSender
    {
        Task SendAsync(string to, string message);
    }
}
