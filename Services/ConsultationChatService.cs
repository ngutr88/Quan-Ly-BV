using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Services
{
    /// <summary>
    /// Conversation state machine + attachment handling shared by both
    /// Areas/Doctor/Controllers/ChatController and Areas/Patient/Controllers/
    /// ChatController, so the two sides of the same chat can never drift on
    /// what "gửi tin" or "đã xem" actually does to a conversation's state.
    /// JSON shaping stays in each controller (the two sides render different
    /// context panels), only the state transitions and file I/O live here.
    /// </summary>
    public class ConsultationChatService
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/heic", "image/heif"
        };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private const int MaxFilesPerMessage = 5;

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ConsultationChatService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<Conversation> GetOrCreateConversationAsync(int benhNhanId, int bacSiId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.BenhNhanId == benhNhanId && c.BacSiId == bacSiId);
            if (conversation != null) return conversation;

            conversation = new Conversation { BenhNhanId = benhNhanId, BacSiId = bacSiId };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        // Chỉ áp dụng khi bác sĩ MỞ hội thoại (không phải khi gửi tin) - gửi
        // tin của bác sĩ tự chuyển DaTraLoi ở AppendMessageAsync bên dưới.
        public async Task MarkOpenedByDoctorAsync(Conversation conversation)
        {
            if (conversation.TrangThai != "Moi") return;
            conversation.TrangThai = "DangXuLy";
            await _context.SaveChangesAsync();
        }

        public async Task<ConversationMessage> AppendMessageAsync(
            Conversation conversation, string senderRole, int? senderUserId, string? noiDung, string loai = "Text")
        {
            var now = DateTime.Now;

            var message = new ConversationMessage
            {
                HoiThoaiId = conversation.Id,
                NguoiGuiId = senderUserId,
                VaiTroNguoiGui = senderRole,
                Loai = loai,
                NoiDung = noiDung,
                ThoiGianGui = now
            };
            _context.ConversationMessages.Add(message);

            conversation.ThoiGianTinNhanCuoi = now;

            if (senderRole == "Doctor")
            {
                conversation.TrangThai = "DaTraLoi";
                conversation.ThoiGianChoTraLoiTu = null;
            }
            else if (senderRole == "Patient")
            {
                if (conversation.TrangThai is "DaTraLoi" or "DaDong")
                {
                    conversation.TrangThai = "Moi";
                }
                conversation.ThoiGianChoTraLoiTu ??= now;
            }
            // "HeThong" (auto-reply): không đổi TrangThai - luôn chèn sau một
            // tin bệnh nhân đã xử lý trạng thái ở nhánh trên.

            await _context.SaveChangesAsync();
            return message;
        }

        // Đánh dấu các tin của PHÍA CÒN LẠI là đã xem. Dùng chung logic với
        // ConsultationChatHub.MarkAsRead (hub xử lý khi cửa sổ đang mở realtime,
        // hàm này xử lý khi tải trang qua HTTP thường - vd F5 hoặc mở lần đầu).
        public async Task MarkSeenAsync(Conversation conversation, string viewerRole)
        {
            var now = DateTime.Now;
            var unseen = await _context.ConversationMessages
                .Where(m => m.HoiThoaiId == conversation.Id && m.VaiTroNguoiGui != viewerRole && !m.DaXemBoiNguoiNhan)
                .ToListAsync();
            if (unseen.Count == 0) return;

            foreach (var m in unseen)
            {
                m.DaXemBoiNguoiNhan = true;
                m.NgayXem = now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task CloseAsync(Conversation conversation, string ghiChuKetLuan)
        {
            conversation.TrangThai = "DaDong";
            conversation.NgayDong = DateTime.Now;
            conversation.GhiChuKetLuan = ghiChuKetLuan.Trim();
            await _context.SaveChangesAsync();
        }

        // Chèn đúng 1 lần / vòng đời hội thoại theo yêu cầu gốc. Gọi sau khi
        // patient đã gửi tin (AppendMessageAsync) để tin auto-reply luôn đứng
        // sau tin vừa nhắn.
        public async Task<ConversationMessage?> TryInsertOffHoursAutoReplyAsync(Conversation conversation, HospitalSettings settings)
        {
            if (conversation.DaGuiAutoReplyNgoaiGio) return null;
            if (Helpers.ConsultationHoursHelper.IsWithinBusinessHours(settings, DateTime.Now)) return null;

            conversation.DaGuiAutoReplyNgoaiGio = true;
            var message = await AppendMessageAsync(
                conversation, "HeThong", null, Helpers.ConsultationChatConstants.AutoReplyOffHoursText, "TuDongPhanHoi");
            return message;
        }

        public (bool IsValid, string? Error) ValidateAttachments(IReadOnlyList<IFormFile> files)
        {
            if (files.Count > MaxFilesPerMessage)
            {
                return (false, $"Chỉ được gửi tối đa {MaxFilesPerMessage} ảnh mỗi tin nhắn.");
            }

            foreach (var file in files)
            {
                if (file.Length > MaxFileSizeBytes)
                {
                    return (false, $"File \"{file.FileName}\" vượt quá 10MB.");
                }
                if (!AllowedContentTypes.Contains(file.ContentType))
                {
                    return (false, $"File \"{file.FileName}\" không phải ảnh jpg/png/heic hợp lệ.");
                }
            }

            return (true, null);
        }

        public async Task SaveAttachmentsAsync(ConversationMessage message, IReadOnlyList<IFormFile> files)
        {
            if (files.Count == 0) return;

            var storageRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "chat-attachments");
            Directory.CreateDirectory(storageRoot);

            var order = 0;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var storedPath = Path.Combine(storageRoot, storedName);
                await using (var stream = File.Create(storedPath))
                {
                    await file.CopyToAsync(stream);
                }

                _context.ConversationMessageAttachments.Add(new ConversationMessageAttachment
                {
                    TinNhanId = message.Id,
                    TenGoc = Path.GetFileName(file.FileName),
                    TenLuuTru = storedName,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    KichThuoc = file.Length,
                    ThuTu = order++
                });
            }

            await _context.SaveChangesAsync();
        }

        public string AttachmentStorageRoot() => Path.Combine(_environment.ContentRootPath, "App_Data", "chat-attachments");
    }
}
