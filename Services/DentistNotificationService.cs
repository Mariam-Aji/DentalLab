using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class DentistNotificationService : IDentistNotificationService
{
    private readonly ApplicationDbContext _db;

    public DentistNotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<DentistNotificationDto>> GetAllAsync(int dentistUserId)
    {
        return await _db.Notifications
            .Where(n => n.RecipientId == dentistUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new DentistNotificationDto
            {
                Id         = n.Id,
                Message    = n.Message,
                Type       = n.Type.ToString(),
                IsRead     = n.IsRead,
                CreatedAt  = n.CreatedAt,
                OrderId    = n.OrderId,
                LabId      = n.LabId,
                BlogPostId = n.BlogPostId,
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int dentistUserId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == dentistUserId);

        if (notification == null) return false;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int dentistUserId)
    {
        await _db.Notifications
            .Where(n => n.RecipientId == dentistUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
