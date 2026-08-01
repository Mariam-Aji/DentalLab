using DentalLab.Api.Data;
using DentalLab.Api.DTOs;
using DentalLab.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // لضمان عمل ToListAsync و AsNoTracking

namespace DentalLab.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task SendAsync(int recipientUserId, string message, NotificationType type,
                                int? orderId = null, int? labId = null)
    {
        var notification = new Notification
        {
            RecipientId = recipientUserId,
            Message = message,
            Type = type,
            OrderId = orderId,
            LabId = labId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Notifications.AddAsync(notification);
        await _db.SaveChangesAsync();

        await _hub.Clients.User(recipientUserId.ToString())
                  .SendAsync("ReceiveNotification", new
                  {
                      notification.Id,
                      notification.Message,
                      Type = notification.Type.ToString(),
                      notification.OrderId,
                      notification.LabId,
                      notification.CreatedAt
                  });
    }

    public async Task<List<NotificationDto>> GetDoctorNotificationsAsync(int doctorId)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == doctorId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                OrderId = n.OrderId,
                LabId = n.LabId,
                BlogPostId = n.BlogPostId
            })
            .ToListAsync();
    }
}