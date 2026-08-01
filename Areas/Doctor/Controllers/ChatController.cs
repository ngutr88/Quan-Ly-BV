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

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
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

        // GET: Doctor/Chat
        public async Task<IActionResult> Index()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound("Bác sĩ không tồn tại trong hệ thống.");

            ViewBag.DoctorProfile = doctor;
            return View();
        }

        // GET: Doctor/Chat/List?filter=ChuaXuLy|DangXuLy|DaDong|TatCa
        [HttpGet]
        public async Task<IActionResult> List(string filter = "ChuaXuLy")
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var settings = _settingsProvider.Load();
            var now = DateTime.Now;

            var query = _context.Conversations
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Where(c => c.BacSiId == doctor.Id)
                .AsQueryable();

            query = filter switch
            {
                "ChuaXuLy" => query.Where(c => c.TrangThai == "Moi"),
                "DangXuLy" => query.Where(c => c.TrangThai == "DangXuLy" || c.TrangThai == "DaTraLoi"),
                "DaDong" => query.Where(c => c.TrangThai == "DaDong"),
                _ => query
            };

            var isWaitingTab = filter is "ChuaXuLy" or "DangXuLy";
            var conversations = await query.ToListAsync();

            var ordered = isWaitingTab
                ? conversations.OrderBy(c => c.ThoiGianChoTraLoiTu ?? DateTime.MaxValue).ToList()
                : conversations.OrderByDescending(c => c.ThoiGianTinNhanCuoi ?? c.NgayTao).ToList();

            // Nhóm "tin gần nhất mỗi hội thoại" ở phía client thay vì
            // GroupBy(...).Select(g => g.First()) - EF Core không dịch được
            // tổ hợp đó sang SQL khi chọn cả entity (chỉ dịch được aggregate
            // đơn giản như Count/Max). Danh sách hội thoại 1 bác sĩ đủ nhỏ để
            // việc này an toàn ở quy mô demo.
            var conversationIds = ordered.Select(c => c.Id).ToList();
            var allMessages = await _context.ConversationMessages
                .Include(m => m.TepDinhKem)
                .Where(m => conversationIds.Contains(m.HoiThoaiId))
                .ToListAsync();
            var lastMessageMap = allMessages
                .GroupBy(m => m.HoiThoaiId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.ThoiGianGui).First());

            var overdueHours = settings.TuVanCamKetPhanHoiTuGio;

            var items = ordered.Select(c =>
            {
                int? waitingHours = c.ThoiGianChoTraLoiTu.HasValue
                    ? (int)Math.Floor((now - c.ThoiGianChoTraLoiTu.Value).TotalHours)
                    : null;

                lastMessageMap.TryGetValue(c.Id, out var lastMessage);

                return new
                {
                    conversationId = c.Id,
                    patientId = c.Patient.Id,
                    patientName = c.Patient.User.HoTen,
                    patientInitials = NameInitialsHelper.GetInitials(c.Patient.User.HoTen),
                    lastMessagePreview = BuildPreview(lastMessage),
                    lastMessageAt = (c.ThoiGianTinNhanCuoi ?? c.NgayTao),
                    trangThai = c.TrangThai,
                    waitingHours,
                    isOverdue = waitingHours.HasValue && waitingHours.Value >= overdueHours
                };
            }).ToList();

            return Json(new { filter, items });
        }

        // GET: Doctor/Chat/Thread?conversationId=
        [HttpGet]
        public async Task<IActionResult> Thread(int conversationId)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var conversation = await _context.Conversations
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.BacSiId == doctor.Id);
            if (conversation == null) return Forbid();

            await _chatService.MarkOpenedByDoctorAsync(conversation);
            await _chatService.MarkSeenAsync(conversation, "Doctor");

            var patient = conversation.Patient;
            var messages = await LoadMessagesAsync(conversationId);

            return Json(new
            {
                conversationId = conversation.Id,
                trangThai = conversation.TrangThai,
                patient = new
                {
                    id = patient.Id,
                    name = patient.User.HoTen,
                    initials = NameInitialsHelper.GetInitials(patient.User.HoTen),
                    age = DateTime.Today.Year - patient.NgaySinh.Year,
                    gender = patient.GioiTinh,
                    bloodType = patient.NhomMau,
                    bhyt = string.IsNullOrEmpty(patient.SoBHYT) ? null : patient.SoBHYT,
                    allergies = string.IsNullOrEmpty(patient.DiUng) ? null : patient.DiUng,
                    history = string.IsNullOrEmpty(patient.TienSuBenh) ? null : patient.TienSuBenh
                },
                messages
            });
        }

        // GET: Doctor/Chat/Contacts?search=
        [HttpGet]
        public async Task<IActionResult> Contacts(string? search)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var patients = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.BacSiId == doctor.Id)
                .Select(a => a.Patient)
                .Distinct()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                patients = patients
                    .Where(p => VietnameseTextHelper.ContainsIgnoreCase(p.User.HoTen, search))
                    .ToList();
            }

            var patientIds = patients.Select(p => p.Id).ToList();
            var existingConversationIds = await _context.Conversations
                .Where(c => c.BacSiId == doctor.Id && patientIds.Contains(c.BenhNhanId))
                .ToDictionaryAsync(c => c.BenhNhanId, c => c.Id);

            var items = patients.Select(p => new
            {
                patientId = p.Id,
                name = p.User.HoTen,
                initials = NameInitialsHelper.GetInitials(p.User.HoTen),
                hasConversation = existingConversationIds.ContainsKey(p.Id),
                conversationId = existingConversationIds.TryGetValue(p.Id, out var id) ? id : (int?)null
            });

            return Json(new { items });
        }

        // POST: Doctor/Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int? conversationId, int? patientId, string? noiDung, List<IFormFile>? files)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

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
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.BacSiId == doctor.Id);
                if (conversation == null) return Forbid();
            }
            else if (patientId.HasValue)
            {
                var ownsPatient = await _context.Appointments
                    .AnyAsync(a => a.BacSiId == doctor.Id && a.BenhNhanId == patientId.Value);
                if (!ownsPatient) return Forbid();

                conversation = await _chatService.GetOrCreateConversationAsync(patientId.Value, doctor.Id);
            }
            else
            {
                return BadRequest(new { message = "Thiếu hội thoại hoặc bệnh nhân." });
            }

            var message = await _chatService.AppendMessageAsync(conversation, "Doctor", doctor.NguoiDungId, noiDung?.Trim());
            await _chatService.SaveAttachmentsAsync(message, files);

            var dto = await BuildMessageDtoAsync(message.Id);
            await _chatNotifier.NotifyMessageReceivedAsync(conversation.Id, dto);
            await _chatNotifier.NotifyConversationStatusChangedAsync(conversation.Id, conversation.TrangThai);

            return Json(dto);
        }

        // POST: Doctor/Chat/Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int conversationId, string ghiChuKetLuan)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            if (string.IsNullOrWhiteSpace(ghiChuKetLuan) || ghiChuKetLuan.Trim().Length < 5)
            {
                return BadRequest(new { message = "Vui lòng nhập ghi chú kết luận (tối thiểu 5 ký tự)." });
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.BacSiId == doctor.Id);
            if (conversation == null) return Forbid();
            if (conversation.TrangThai == "DaDong")
            {
                return BadRequest(new { message = "Hội thoại đã được đóng trước đó." });
            }

            await _chatService.CloseAsync(conversation, ghiChuKetLuan);
            await _chatNotifier.NotifyConversationStatusChangedAsync(conversation.Id, conversation.TrangThai);
            await _dashboardNotifier.NotifyChatUpdatedAsync(doctor.Id);

            return Json(new { success = true, trangThai = conversation.TrangThai });
        }

        // POST: Doctor/Chat/InviteBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteBooking(int conversationId)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.BacSiId == doctor.Id);
            if (conversation == null) return Forbid();

            var message = await _chatService.AppendMessageAsync(
                conversation, "Doctor", doctor.NguoiDungId, ConsultationChatConstants.InviteBookingMessageText, "MoiDatLich");

            var dto = await BuildMessageDtoAsync(message.Id);
            await _chatNotifier.NotifyMessageReceivedAsync(conversation.Id, dto);
            await _chatNotifier.NotifyConversationStatusChangedAsync(conversation.Id, conversation.TrangThai);

            return Json(dto);
        }

        // GET: Doctor/Chat/UnreadCount - polled after a SignalR "ChatUpdated" signal.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return Json(new { count = 0 });

            var count = await ChatUnreadCountHelper.GetUnreadCountForDoctorAsync(_context, doctor.Id);
            return Json(new { count });
        }

        // GET: Doctor/Chat/Attachment/5
        [HttpGet]
        public async Task<IActionResult> Attachment(int id)
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var attachment = await _context.ConversationMessageAttachments
                .Include(a => a.TinNhan).ThenInclude(m => m.HoiThoai)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attachment == null || attachment.TinNhan.HoiThoai.BacSiId != doctor.Id) return Forbid();

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
                url = Url.Action("Attachment", "Chat", new { area = "Doctor", id = a.Id })
            })
        };

        private static string? BuildPreview(ConversationMessage? message)
        {
            if (message == null) return null;
            if (!string.IsNullOrWhiteSpace(message.NoiDung)) return message.NoiDung;
            return message.TepDinhKem?.Count > 0 ? "[Hình ảnh]" : null;
        }

        private async Task<QuanLyBenhVien.Models.Doctor?> ResolveDoctorAsync()
        {
            var currentUserId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);

            if (doctor == null)
            {
                var identityValue = User.Identity?.Name;
                doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.HoTen == identityValue || d.User.Email == identityValue);
            }

            return doctor;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
