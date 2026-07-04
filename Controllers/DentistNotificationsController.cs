using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/dentist/notifications")]
[Authorize(Roles = "Dentist")]
public class DentistNotificationsController : ControllerBase
{
    private readonly IDentistNotificationService _service;

    public DentistNotificationsController(IDentistNotificationService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/dentist/notifications
    /// كل إشعارات الطبيب (طلبيات + اتصالات + بلوق) مرتبة من الأحدث
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var notifications = await _service.GetAllAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// PUT api/dentist/notifications/{id}/read
    /// تحديد إشعار محدد كمقروء
    /// </summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        var success = await _service.MarkAsReadAsync(id, userId);
        if (!success) return NotFound(new { message = "الإشعار غير موجود." });
        return Ok(new { message = "تم تحديد الإشعار كمقروء." });
    }

    /// <summary>
    /// PUT api/dentist/notifications/read-all
    /// تحديد كل الإشعارات كمقروءة
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _service.MarkAllAsReadAsync(userId);
        return Ok(new { message = "تم تحديد كل الإشعارات كمقروءة." });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException());
}
