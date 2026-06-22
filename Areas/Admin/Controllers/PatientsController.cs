using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Patients
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Patients
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.User.HoTen.Contains(searchString) || 
                                         p.User.Sdt.Contains(searchString) || 
                                         p.SoBHYT.Contains(searchString));
            }

            var patients = await query.ToListAsync();
            ViewBag.SearchString = searchString;

            return View(patients);
        }

        // GET: Admin/Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            // Historical Appointments of this patient
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id)
                .OrderByDescending(a => a.ThoiGian)
                .ToListAsync();

            // Historical Examination Records (Medical History)
            ViewBag.ExaminationRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            // Historical Invoices & payment status
            ViewBag.Invoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            return View(patient);
        }
    }
}
