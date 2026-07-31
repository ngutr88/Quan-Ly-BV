using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class SupportController : Controller
    {
        private readonly HospitalSettingsProvider _settingsProvider;

        public SupportController(HospitalSettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public IActionResult Index() => View(_settingsProvider.Load());
    }
}
