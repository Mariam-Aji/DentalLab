using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Data;
using DentalLab.Api.Dtos.Complaints;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Services.Interfaces;

namespace DentalLab.Api.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly ApplicationDbContext _context;
        private readonly IComplaintRepository _complaintRepository;

        public ComplaintService(ApplicationDbContext context, IComplaintRepository complaintRepository)
        {
            _context = context;
            _complaintRepository = complaintRepository;
        }

        // دالة مساعدة لعمل Mapping وتحويل الـ Complaint إلى ComplaintDetailsDto (حقول الجدول فقط)
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
                // تم استبعاد AdminId
                CreatedAtUtc = c.CreatedAtUtc,
                Reply = c.Reply,
                RepliedAtUtc = c.RepliedAtUtc,
                // إذا كان هناك رد، نحدد أن المصدر هو الإدارة، وإلا يكون null أو غير محدد
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

            if (targetLabId.HasValue)
            {
                complaint.Destination = ComplaintDestination.Lab;
                complaint.TargetLabId = targetLabId.Value;
                complaint.AdminId = null;

                var labUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == targetLabId.Value);

                if (labUser != null)
                {
                    targetName = labUser.NamePlace ?? "مخبر بدون اسم";
                    recipientId = labUser.Id;
                }
                else
                {
                    throw new Exception("المخبر المستهدف غير موجود.");
                }
            }
            else
            {
                complaint.Destination = ComplaintDestination.Admin;
                complaint.TargetLabId = null;
                complaint.AdminId = 1;
                targetName = "إدارة النظام (Admin)";
                recipientId = 1;
            }

            await _complaintRepository.AddAsync(complaint);
            await _complaintRepository.SaveChangesAsync();

            var notification = new Notification
            {
                RecipientId = recipientId,
                Message = $"لديك شكوى جديدة بعنوان: '{dto.Title}'",
                Type = NotificationType.InfoRequested,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                LabId = targetLabId
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            return new ComplaintResponseDto
            {
                Id = complaint.Id,
                Title = complaint.Title,
                Text = complaint.Text,
                Destination = complaint.Destination,
                TargetName = targetName,
                CreatedAtUtc = complaint.CreatedAtUtc
            };
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // جلب شكاوى الإدارة وإرجاعها كـ DTO يحتوي حقول الجدول فقط
        public async Task<List<ComplaintDetailsDto>> GetAdminComplaintsAsync()
        {
            var complaints = await _complaintRepository.GetAdminComplaintsAsync();
            return complaints.Select(MapToDto).ToList();
        }

        // جلب شكاوى المخابر وإرجاعها كـ DTO يحتوي حقول الجدول فقط
        public async Task<List<ComplaintDetailsDto>> GetLabComplaintsAsync()
        {
            var complaints = await _complaintRepository.GetLabComplaintsAsync();
            return complaints.Select(MapToDto).ToList();
        }

        // جلب شكاوى الطبيب الخاص به وإرجاعها كـ DTO يحتوي حقول الجدول فقط
        public async Task<List<ComplaintDetailsDto>> GetDentistComplaintsAsync(int dentistId)
        {
            var complaints = await _complaintRepository.GetDentistComplaintsAsync(dentistId);
            return complaints.Select(MapToDto).ToList();
        }
        public async Task<ComplaintDetailsDto> ReplyToComplaintAsync(int dentistId, int complaintId, ReplyComplaintDto dto)
        {
            var complaint = await _complaintRepository.GetByIdAsync(complaintId);
            if (complaint == null)
            {
                throw new Exception("الشكوى غير موجودة.");
            }

            // التحقق من أن الشكوى تخص الطبيب الممرر في الراوت لمنع أي تلاعب
            if (complaint.UserId != dentistId)
            {
                throw new Exception("معرف الطبيب المستلم لا يتطابق مع صاحب الشكوى.");
            }

            // تحديث حقول الرد وتاريخ الرد
            complaint.Reply = dto.ReplyText;
            complaint.RepliedAtUtc = DateTime.UtcNow;

            await _complaintRepository.SaveChangesAsync();

            // إنشاء إشعار موجه للطبيب (RecipientId = dentistId) متضمناً معرف الشكوى أو تفاصيلها
            var notification = new Notification
            {
                RecipientId = dentistId,
                Message = $"تم الرد على شكواك رقم ({complaint.Id}) بعنوان: '{complaint.Title}'",
                Type = NotificationType.InfoRequested, // أو أي نوع مناسب لديك
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            return MapToDto(complaint);
        }
    }
}