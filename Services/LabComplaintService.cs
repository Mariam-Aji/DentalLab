using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabComplaintService : ILabComplaintService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public LabComplaintService(ApplicationDbContext db, INotificationService notifications)
    {
        _db            = db;
        _notifications = notifications;
    }

    public async Task<(List<LabComplaintDto>? result, string? error)> GetLabComplaintsAsync(int userId)
    {
        var labId = await GetLabIdAsync(userId);
        if (labId == null) return (null, "Lab not found.");

        var complaints = await _db.Complaints
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.Destination == ComplaintDestination.Lab && c.TargetLabId == labId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new LabComplaintDto
            {
                Id           = c.Id,
                Title        = c.Title,
                Text         = c.Text,
                DentistId    = c.UserId,
                DentistName  = c.User.Name,
                CreatedAtUtc = c.CreatedAtUtc,
                Reply        = c.Reply,
                RepliedAtUtc = c.RepliedAtUtc
            })
            .ToListAsync();

        return (complaints, null);
    }

    public async Task<(LabComplaintDto? result, string? error)> GetComplaintByIdAsync(int userId, int complaintId)
    {
        var labId = await GetLabIdAsync(userId);
        if (labId == null) return (null, "Lab not found.");

        var complaint = await _db.Complaints
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c =>
                c.Id == complaintId &&
                c.Destination == ComplaintDestination.Lab &&
                c.TargetLabId == labId)
            .Select(c => new LabComplaintDto
            {
                Id           = c.Id,
                Title        = c.Title,
                Text         = c.Text,
                DentistId    = c.UserId,
                DentistName  = c.User.Name,
                CreatedAtUtc = c.CreatedAtUtc,
                Reply        = c.Reply,
                RepliedAtUtc = c.RepliedAtUtc
            })
            .FirstOrDefaultAsync();

        if (complaint == null)
            return (null, "Complaint not found.");

        return (complaint, null);
    }

    public async Task<(LabComplaintDto? result, string? error)> ReplyToComplaintAsync(
        int userId, int complaintId, LabComplaintReplyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reply))
            return (null, "Reply cannot be empty.");

        var labId = await GetLabIdAsync(userId);
        if (labId == null) return (null, "Lab not found.");

        var complaint = await _db.Complaints
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.Id == complaintId &&
                c.Destination == ComplaintDestination.Lab &&
                c.TargetLabId == labId);

        if (complaint == null)
            return (null, "Complaint not found.");

        if (complaint.Reply != null)
            return (null, "Already replied to this complaint.");

        complaint.Reply        = dto.Reply.Trim();
        complaint.RepliedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // إشعار الطبيب بوجود رد على شكواه مع id المخبر
        await _notifications.SendAsync(
            recipientUserId: complaint.UserId,
            message: $"المخبر رد على شكواك \"{complaint.Title}\".",
            type: NotificationType.ComplaintReply,
            labId: labId
        );

        return (new LabComplaintDto
        {
            Id           = complaint.Id,
            Title        = complaint.Title,
            Text         = complaint.Text,
            DentistId    = complaint.UserId,
            DentistName  = complaint.User.Name,
            CreatedAtUtc = complaint.CreatedAtUtc,
            Reply        = complaint.Reply,
            RepliedAtUtc = complaint.RepliedAtUtc
        }, null);
    }

    // ─── Helper ──────────────────────────────────────────────────────────

    private async Task<int?> GetLabIdAsync(int userId) =>
        await _db.Labs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
}
