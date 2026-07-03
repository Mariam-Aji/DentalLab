using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface IDentistNotificationService
{
    /// <summary>كل إشعارات الطبيب مرتبة من الأحدث للأقدم</summary>
    Task<List<DentistNotificationDto>> GetAllAsync(int dentistUserId);

    /// <summary>تحديد إشعار كمقروء</summary>
    Task<bool> MarkAsReadAsync(int notificationId, int dentistUserId);

    /// <summary>تحديد كل الإشعارات كمقروءة</summary>
    Task MarkAllAsReadAsync(int dentistUserId);
}
