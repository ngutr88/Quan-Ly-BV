using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    // Split out of RecordController so "Tài liệu của tôi" can be toggled
    // independently in the permission matrix (ModulePermissionFilter keys
    // strictly per-controller) - the upload/download/delete logic itself is
    // unchanged, only relocated.
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Patient/Documents
        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var documents = await _context.PatientDocuments
                .Where(d => d.BenhNhanId == patient.Id)
                .OrderByDescending(d => d.NgayTaiLen)
                .ToListAsync();

            return View(documents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file, string tenTaiLieu, string ghiChu)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension) || file.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ PDF/JPG/PNG và dung lượng tối đa 10MB.";
                return RedirectToAction(nameof(Index));
            }

            var storageRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents");
            Directory.CreateDirectory(storageRoot);
            var storedName = $"{System.Guid.NewGuid():N}{extension}";
            var storedPath = Path.Combine(storageRoot, storedName);
            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            _context.PatientDocuments.Add(new PatientDocument
            {
                BenhNhanId = patient.Id,
                TenTaiLieu = string.IsNullOrWhiteSpace(tenTaiLieu) ? Path.GetFileName(file.FileName) : tenTaiLieu.Trim(),
                LoaiTaiLieu = "GiayToKhamBenh",
                TenLuuTru = storedName,
                ContentType = file.ContentType ?? "application/octet-stream",
                KichThuoc = file.Length,
                GhiChu = ghiChu?.Trim() ?? string.Empty
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu tài liệu khám bệnh.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id && d.BenhNhanId == patient.Id);
            return await ServeDocumentAsync(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id && d.BenhNhanId == patient.Id);
            if (document != null)
            {
                var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                _context.PatientDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ServeDocumentAsync(PatientDocument? document)
        {
            if (document == null) return NotFound();
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "patient-documents", document.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, document.ContentType, document.TenTaiLieu);
        }

        private async Task<QuanLyBenhVien.Models.Patient?> GetCurrentPatientAsync()
        {
            var userId = GetCurrentUserId();
            return await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == userId);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
