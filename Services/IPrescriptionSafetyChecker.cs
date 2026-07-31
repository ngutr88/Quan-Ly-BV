using System.Threading.Tasks;
using QuanLyBenhVien.Models.ViewModels.PrescriptionSafety;

namespace QuanLyBenhVien.Services
{
    public interface IPrescriptionSafetyChecker
    {
        Task<PrescriptionSafetyResult> CheckAsync(PrescriptionSafetyContext context);
    }
}
