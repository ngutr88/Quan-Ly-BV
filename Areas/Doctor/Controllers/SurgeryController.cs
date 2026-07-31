using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    [TypeFilter(typeof(DoctorMenuConditionFilter), Arguments = new object[] { DoctorMenuConditions.SurgicalSpecialty })]
    public class SurgeryController : Controller
    {
        public IActionResult Index() => View();
    }
}
