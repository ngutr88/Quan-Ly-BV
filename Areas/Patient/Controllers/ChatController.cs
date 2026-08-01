using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ConsultationChatService _chatService;
        private readonly ConsultationChatNotifier _chatNotifier;
        private readonly DoctorDashboardNotifier _dashboardNotifier;
        private readonly HospitalSettingsProvider _settingsProvider;

        public ChatController(
            ApplicationDbContext context,
            ConsultationChatService chatService,
            ConsultationChatNotifier chatNotifier,
            DoctorDashboardNotifier dashboardNotifier,
            HospitalSettingsProvider settingsProvider)
        {
            _context = context;
            _chatService = chatService;
            _chatNotifier = chatNotifier;
            _dashboardNotifier = dashboardNotifier;
            _settingsProvider = settingsProvider;
        }

        // GET: Patient/Chat
        public async Task<IActionResult> Index()
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound("Không tìm thấy hồ sơ bệnh nhân.");

            var settings = _settingsProvider.Load();
            ViewBag.PatientProfile = patient;
            ViewBag.HotlineCapCuu = settings.HotlineCapCuu;
            ViewBag.CamKetText =
                $"Tin nhắn được trả lời trong giờ hành chính ({settings.TuVanGioBatDau}–{settings.TuVanGioKetThuc}, " +
                $"{VietnameseDayLabel(settings.TuVanNgayApDungTu)}–{VietnameseDayLabel(settings.TuVanNgayApDungDen)}), " +
                $"thường trong vòng {settings.TuVanCamKetPhanHoiTuGio}–{settings.TuVanCamKetPhanHoiDenGio} giờ.";

            return View();
        }

        // GET: Patient/Chat/List
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound();

            var conversations = await _context.Conversations
                .Include(c => c.Doctor).ThenInclude(d => d.User)
                .Where(c => c.BenhNhanId == patient.Id)
                .ToListAsync();

            var ordered = conversations.OrderByDescending(c => c.ThoiGianTinNhanCuoi ?? c.NgayTao).ToList();

            var conversationIds = ordered.Select(c => c.Id).ToList();
            var allMessages = await _context.ConversationMessages
                .Include(m => m.TepDinhKem)
                .Where(m => conversationIds.Contains(m.HoiThoaiId))
                .ToListAsync();
            var lastMessageMap = allMessages
                .GroupBy(m => m.HoiThoaiId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.ThoiGianGui).First());
            var hasUnreadMap = allMessages
                .Where(m => m.VaiTroNguoiGui == "Doctor" && !m.DaXemBoiNguoiNhan)
                .Select(m => m.HoiThoaiId)
                .ToHashSet();

            var items = ordered.Select(c =>
            {
                lastMessageMap.TryGetValue(c.Id, out var lastMessage);
                return new
                {
                    conversationId = c.Id,
                    doctorId = c.Doctor.Id,
                    doctorName = DoctorDisplayHelper.FormatDoctorName(c.Doctor),
                    doctorInitials = NameInitialsHelper.GetInitials(c.Doctor.User.HoTen),
                    lastMessagePreview = BuildPreview(lastMessage),
                    lastMessageAt = (c.ThoiGianTinNhanCuoi ?? c.NgayTao),
                    trangThai = c.TrangThai,
                    hasUnread = hasUnreadMap.Contains(c.Id)
                };
            }).ToList();

            return Json(new { items });
        }

        // GET: Patient/Chat/Thread?conversationId=
        [HttpGet]
        public async Task<IActionResult> Thread(int conversationId)
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound();

            var conversation = await _context.Conversations
                .Include(c => c.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.BenhNhanId == patient.Id);
            if (conversation == null) return Forbid();

            await _chatService.MarkSeenAsync(conversation, "Patient");

            var doctor = conversation.Doctor;
            var messages = await LoadMessagesAsync(conversationId);

            return Json(new
            {
                conversationId = conversation.Id,
                trangThai = conversation.TrangThai,
                doctor = new
                {
                    id = doctor.Id,
                    khoaId = doctor.KhoaId,
                    name = DoctorDisplayHelper.FormatDoctorName(doctor),
                    initials = NameInitialsHelper.GetInitials(doctor.User.HoTen),
                    chuyenKhoa = doctor.ChuyenKhoa
                },
                messages
            });
        }

        // GET: Patient/Chat/Contacts?search=
        [HttpGet]
        public async Task<IActionResult> Contacts(string? search)
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound();

            var doctors = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.BenhNhanId == patient.Id)
                .Select(a => a.Doctor)
                .Distinct()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                doctors = doctors
                    .Where(d => VietnameseTextHelper.ContainsIgnoreCase(d.User.HoTen, search))
                    .ToList();
            }

            var doctorIds = doctors.Select(d => d.Id).ToList();
            var existingConversationIds = await _context.Conversations
                .Where(c => c.BenhNhanId == patient.Id && doctorIds.Contains(c.BacSiId))
                .ToDictionaryAsync(c => c.BacSiId, c => c.Id);

            var items = doctors.Select(d => new
            {
                doctorId = d.Id,
                name = DoctorDisplayHelper.FormatDoctorName(d),
                chuyenKhoa = d.ChuyenKhoa,
                initials = NameInitialsHelper.GetInitials(d.User.HoTen),
                hasConversation = existingConversationIds.ContainsKey(d.Id),
                conversationId = existingConversationIds.TryGetValue(d.Id, out var id) ? id : (int?)null
            });

            return Json(new { items });
        }

        // POST: Patient/Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int? conversationId, int? doctorId, string? noiDung, List<IFormFile>? files)
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound();

            files ??= new List<IFormFile>();
            if (string.IsNullOrWhiteSpace(noiDung) && files.Count == 0)
            {
                return BadRequest(new { message = "Tin nhắn cần có nội dung hoặc ảnh đính kèm." });
            }

            var (filesValid, fileError) = _chatService.ValidateAttachments(files);
            if (!filesValid) return BadRequest(new { message = fileError });

            Conversation? conversation = null;
            if (conversationId.HasValue)
            {
                conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.BenhNhanId == patient.Id);
                if (conversation == null) return Forbid();
            }
            else if (doctorId.HasValue)
            {
                var hasHistory = await _context.Appointments
                    .AnyAsync(a => a.BenhNhanId == patient.Id && a.BacSiId == doctorId.Value);
                if (!hasHistory) return Forbid();

                conversation = await _chatService.GetOrCreateConversationAsync(patient.Id, doctorId.Value);
            }
            else
            {
                return BadRequest(new { message = "Thiếu hội thoại hoặc bác sĩ." });
            }

            var message = await _chatService.AppendMessageAsync(conversation, "Patient", patient.NguoiDungId, noiDung?.Trim());
            await _chatService.SaveAttachmentsAsync(message, files);

            var dto = await BuildMessageDtoAsync(message.Id);
            await _chatNotifier.NotifyMessageReceivedAsync(conversation.Id, dto);
            await _chatNotifier.NotifyConversationStatusChangedAsync(conversation.Id, conversation.TrangThai);
            await _dashboardNotifier.NotifyChatUpdatedAsync(conversation.BacSiId);

            // Ngoài giờ hành chính: chèn đúng 1 lần / vòng đời hội thoại.
            var settings = _settingsProvider.Load();
            var autoReply = await _chatService.TryInsertOffHoursAutoReplyAsync(conversation, settings);
            if (autoReply != null)
            {
                var autoReplyDto = await BuildMessageDtoAsync(autoReply.Id);
                await _chatNotifier.NotifyMessageReceivedAsync(conversation.Id, autoReplyDto);
            }

            return Json(dto);
        }

        // GET: Patient/Chat/UnreadCount
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return Json(new { count = 0 });

            var count = await ChatUnreadCountHelper.GetUnreadCountForPatientAsync(_context, patient.Id);
            return Json(new { count });
        }

        // GET: Patient/Chat/Attachment/5
        [HttpGet]
        public async Task<IActionResult> Attachment(int id)
        {
            var patient = await ResolvePatientAsync();
            if (patient == null) return NotFound();

            var attachment = await _context.ConversationMessageAttachments
                .Include(a => a.TinNhan).ThenInclude(m => m.HoiThoai)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attachment == null || attachment.TinNhan.HoiThoai.BenhNhanId != patient.Id) return Forbid();

            var path = Path.Combine(_chatService.AttachmentStorageRoot(), attachment.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, attachment.ContentType, attachment.TenGoc);
        }

        private async Task<List<object>> LoadMessagesAsync(int conversationId)
        {
            var messages = await _context.ConversationMessages
                .Include(m => m.TepDinhKem)
                .Where(m => m.HoiThoaiId == conversationId)
                .OrderBy(m => m.ThoiGianGui)
                .Take(200)
                .ToListAsync();

            return messages.Select(ToMessageDto).ToList<object>();
        }

        private async Task<object> BuildMessageDtoAsync(int messageId)
        {
            var message = await _context.ConversationMessages
                .Include(m => m.TepDinhKem)
                .FirstAsync(m => m.Id == messageId);
            return ToMessageDto(message);
        }

        private object ToMessageDto(ConversationMessage m) => new
        {
            id = m.Id,
            conversationId = m.HoiThoaiId,
            vaiTro = m.VaiTroNguoiGui,
            noiDung = m.NoiDung,
            loai = m.Loai,
            thoiGianGui = m.ThoiGianGui,
            daXem = m.DaXemBoiNguoiNhan,
            attachments = m.TepDinhKem.OrderBy(a => a.ThuTu).Select(a => new
            {
                id = a.Id,
                tenGoc = a.TenGoc,
                contentType = a.ContentType,
                url = Url.Action("Attachment", "Chat", new { area = "Patient", id = a.Id })
            })
        };

        private static string? BuildPreview(ConversationMessage? message)
        {
            if (message == null) return null;
            if (!string.IsNullOrWhiteSpace(message.NoiDung)) return message.NoiDung;
            return message.TepDinhKem?.Count > 0 ? "[Hình ảnh]" : null;
        }

        private static string VietnameseDayLabel(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            _ => "Chủ Nhật"
        };

        private async Task<QuanLyBenhVien.Models.Patient?> ResolvePatientAsync()
        {
            var currentUserId = GetCurrentUserId();
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.NguoiDungId == currentUserId);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
