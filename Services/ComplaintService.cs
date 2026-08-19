using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DentalLab.Api.Data;
using DentalLab.Api.Dtos.Complaints;
using DentalLab.Api.Models;
//using DentalLab.Api.Hubs;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Services.Interfaces;

namespace DentalLab.Api.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly ApplicationDbContext _context;
        private readonly IComplaintRepository _complaintRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ComplaintService(
            ApplicationDbContext context,
            IComplaintRepository complaintRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _complaintRepository = complaintRepository;
            _hubContext = hubContext;
        }

        private ComplaintDetailsDto MapToDto(Complaint c)
        {
            return new ComplaintDetailsDto
            {
                Id = c.Id,
                Destination = c.Destination,
                Title = c.Title,
                Text = c.Text,
                UserId = c.UserId,
                TargetLabId = c.TargetLabId,
                CreatedAtUtc = c.CreatedAtUtc,
                Reply = c.Reply,
                RepliedAtUtc = c.RepliedAtUtc,
                RepliedBy = !string.IsNullOrEmpty(c.Reply) ? "الإدارة" : null
            };
        }

        public async Task<ComplaintResponseDto> CreateComplaintAsync(int userId, int? targetLabId, CreateComplaintDto dto)
        {
            var complaint = new Complaint
            {
                Title = dto.Title,
                Text = dto.Text,
                UserId = userId
            };

            string targetName = "الإدارة";
            int recipientId = 1;

            // إذا تم إرسال معرف ضمن الراوت، فالشكوى موجهة لمخبر
            if (targetLabId.HasValue)
            {
                complaint.Destination = ComplaintDestination.Lab;
                complaint.TargetLabId = targetLabId.Value;
                complaint.AdminId = null;

                // 1. البحث ضمن جدول المخابر (Labs) عن المخبر المطابق مع جلب صاحب المخبر (Owner / User)
                var labRecord = await _context.Labs
                    .Include(l => l.Owner) // جلب جدول المستخدمين المرتبط بالمخبر
                    .FirstOrDefaultAsync(l => l.Id == targetLabId.Value);

                if (labRecord != null)
                {
                    // 2. الحصول على معرف المستخدم (UserId) وصاحب المخبر
                    recipientId = labRecord.UserId;

                    // 3. الحصول على اسم المكان (NamePlace) من جدول المستخدمين (Owner)
                    targetName = labRecord.Owner?.NamePlace ?? labRecord.Owner?.Name ?? "مخبر بدون اسم";
                }
                else
                {
                    throw new Exception("المخبر المستهدف غير موجود.");
                }
            }
            else // إذا لم يتم إرسال معرف ضمن الراوت، فالشكوى موجهة للأدمن حصراً
            {
                complaint.Destination = ComplaintDestination.Admin;
                complaint.TargetLabId = null;
                complaint.AdminId = 1;
                targetName = "إدارة النظام (Admin)";
                recipientId = 1;
            }

            await _complaintRepository.AddAsync(complaint);
            await _complaintRepository.SaveChangesAsync();

            // بناء ريسبونس الشكوى
            var complaintResponse = new ComplaintResponseDto
            {
                Id = complaint.Id,
                Title = complaint.Title,
                Text = complaint.Text,
                Destination = complaint.Destination,
                TargetName = targetName,
                CreatedAtUtc = complaint.CreatedAtUtc
            };

            // حفظ الإشعار في قاعدة البيانات للمستقبل الصحيح (سواء المخبر أو الأدمن)
            var notification = new Notification
            {
                RecipientId = recipientId,
                Message = $"شكوى جديدة: {complaint.Title}",
                Type = NotificationType.InfoRequested,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                LabId = targetLabId
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // محتوى الإشعار المرسل عبر SignalR ليطابق ريسبونس الشكوى تماماً
            var notificationPayload = new
            {
                notification.Id,
                notification.Type,
                notification.CreatedAt,
                Data = complaintResponse
            };

            // إرسال الإشعار عبر SignalR للمستخدم المرتبط بالمخبر أو الأدمن
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("ReceiveOrderNotification", notificationPayload);

            return complaintResponse;
        }
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ComplaintDetailsDto>> GetAdminComplaintsAsync()
        {
            var complaints = await _complaintRepository.GetAdminComplaintsAsync();
            return complaints.Select(MapToDto).ToList();
        }

        public async Task<List<ComplaintDetailsDto>> GetLabComplaintsAsync()
        {
            var complaints = await _complaintRepository.GetLabComplaintsAsync();
            return complaints.Select(MapToDto).ToList();
        }

        public async Task<List<ComplaintDetailsDto>> GetDentistComplaintsAsync(int dentistId)
        {
            var complaints = await _complaintRepository.GetDentistComplaintsAsync(dentistId);
            return complaints.Select(MapToDto).ToList();
        }

        public async Task<ComplaintDetailsDto> ReplyToComplaintAsync(int adminOrReplierId, int complaintId, ReplyComplaintDto dto)
        {
            var complaint = await _complaintRepository.GetByIdAsync(complaintId);
            if (complaint == null)
            {
                throw new Exception("الشكوى غير موجودة.");
            }

            complaint.Reply = dto.ReplyText;
            complaint.RepliedAtUtc = DateTime.UtcNow;

            await _complaintRepository.SaveChangesAsync();

            // صاحب الشكوى الأصلي (الذي سيصله الرد)
            int targetRecipientId = complaint.UserId;

            var complaintResponse = MapToDto(complaint);

            var notification = new Notification
            {
                RecipientId = targetRecipientId,
                Message = $"تم الرد على شكواك: {complaint.Title}",
                Type = NotificationType.InfoRequested,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Payload الرد عبر SignalR ليطابق بنية البيانات المطلوبة
            var replyPayload = new
            {
                notification.Id,
                notification.Type,
                notification.CreatedAt,
                Data = complaintResponse
            };

            await _hubContext.Clients.User(targetRecipientId.ToString())
                .SendAsync("ReceiveOrderNotification", replyPayload);

            return complaintResponse;
        }
    }
}