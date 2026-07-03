using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

/// <summary>
/// خدمة مركزية لحفظ الإشعارات في DB وبثها فوراً عبر SignalR.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// يحفظ الإشعار في قاعدة البيانات ويبثه للمستخدم المستهدف عبر SignalR.
    /// </summary>
    Task SendAsync(int recipientUserId, string message, NotificationType type,
                   int? orderId = null, int? labId = null);
}
