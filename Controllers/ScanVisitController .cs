using DentalLab.Api.DTOs;
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
    [Authorize(Roles = "Dentist")]
    [HttpGet("available-slots/{labId}")]
    public async Task<IActionResult> GetAvailableSlots(int labId)
    {
        var slots = await _service.GetAvailableSlotsAsync(labId, DateTime.Today);

        if (slots == null || !slots.Any())
        {
            return NotFound(new { message = "لا توجد مواعيد فحص متاحة لهذا المخبر حالياً." });
        }

        return Ok(slots);
    }
    //
    [Authorize(Roles = "Dentist")]
    [HttpGet("my-bookings")]
    public async Task<ActionResult<List<DentistScanVisitDto>>> GetMyBookings()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out int dentistId))
        {
            return Unauthorized(new { message = "رمز المعرف في التوكن غير صالح." });
        }

        var bookings = await _service.GetDentistScanVisitsAsync(dentistId);
        return Ok(bookings);
    }
}