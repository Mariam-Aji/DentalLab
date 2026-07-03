using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace DentalLab.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task SendAsync(int recipientUserId, string message, NotificationType type,
                                int? orderId = null, int? labId = null)
    {
        var notification = new Notification
        {
            RecipientId = recipientUserId,
            Message     = message,
            Type        = type,
            OrderId     = orderId,
            LabId       = labId,
            IsRead      = false,
            CreatedAt   = DateTime.UtcNow
        };

        await _db.Notifications.AddAsync(notification);
        await _db.SaveChangesAsync();

        await _hub.Clients.User(recipientUserId.ToString())
                  .SendAsync("ReceiveNotification", new
                  {
                      notification.Id,
                      notification.Message,
                      Type      = notification.Type.ToString(),
                      notification.OrderId,
                      notification.LabId,
                      notification.CreatedAt
                  });
    }
}
