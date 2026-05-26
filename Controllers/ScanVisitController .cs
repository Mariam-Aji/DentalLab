using DentalLab.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/scan-visits")]
public class ScanVisitController : ControllerBase
{
    private readonly IScanVisitService _service;

    public ScanVisitController(IScanVisitService service)
    {
        _service = service;
    }

    // 🔹 إضافة موعد من المخبر
    [HttpPost("add-slot/{labId}")]
    public async Task<IActionResult> AddSlot(int labId, DateTime date, TimeSpan time, SlotPeriod period)
    {
        await _service.AddSlotAsync(labId, date, time, period);
        return Ok(new { message = "Slot created successfully" });
    }

    // 🔹 عرض المواعيد المتاحة فقط
    [HttpGet("available/{labId}")]
    public async Task<IActionResult> GetAvailable(int labId)
    {
        var result = await _service.GetAvailableSlotsAsync(labId, DateTime.UtcNow);
        return Ok(result);
    }

    // 🔹 حجز موعد (محمية لضمان قراءة التوكن بأمان)
    [Authorize(Roles = "Dentist")]
    [HttpPost("book/{labId}/{slotId}")]
    public async Task<IActionResult> Book(int labId, int slotId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int dentistId))
        {
            return Unauthorized(new { message = "لم يتم العثور على هوية الطبيب، يرجى إرسال توكن صالح." });
        }

        var success = await _service.BookSlotAsync(dentistId, labId, slotId);

        if (!success)
            return BadRequest(new { message = "هذا الموعد محجوز مسبقاً أو غير متاح حالياً." });

        return Ok(new { message = "تم الحجز بنجاح وتم إرسال الإشعار بالتفاصيل للمخبر المَعني." });
    }
    [Authorize(Roles = "Lab")]
    [HttpGet("my-notifications")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int labOwnerId))
        {
            return Unauthorized(new { message = "غير مسموح بالدخول، التوكن غير صالح." });
        }

        var notifications = await _service.GetLabNotificationsAsync(labOwnerId);
        return Ok(notifications);
    }
}