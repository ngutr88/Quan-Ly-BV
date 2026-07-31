using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    [TypeFilter(typeof(DoctorMenuConditionFilter), Arguments = new object[] { DoctorMenuConditions.WardAssignment })]
    public class InpatientController : Controller
    {
        public IActionResult Index() => View();
    }
}
