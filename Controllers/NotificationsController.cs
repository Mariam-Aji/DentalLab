using System.Security.Claims;
using DentalLab.Api.DTOs;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// جلب الإشعارات الخاصة بالطبيب المسجّل بناءً على التوكن
    /// </summary>
    [HttpGet("doctor")]
    public async Task<ActionResult<List<NotificationDto>>> GetDoctorNotifications()
    {
        // 1. استخراج معرف الطبيب تلقائياً من Claims التوكن
        var doctorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        // 2. التحقق من وجود المعرف وصحته
        if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out int doctorId))
        {
            return Unauthorized(new { message = "التوكن غير صالح أو هوية الطبيب مفقودة." });
        }

        // 3. استدعاء السيرفس لجلب الإشعارات
        var notifications = await _notificationService.GetDoctorNotificationsAsync(doctorId);

        // 4. إرجاع النتيجة
        return Ok(notifications);
    }
}