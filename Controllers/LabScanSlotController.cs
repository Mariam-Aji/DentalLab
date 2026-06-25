using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab/scan-slots")]
[Authorize(Roles = "Lab")]
public class LabScanSlotController : ControllerBase
{
    private readonly ILabScanSlotService _service;

    public LabScanSlotController(ILabScanSlotService service)
    {
        _service = service;
    }

    /// <summary>
    /// عرض كل مواعيد المسح للمخبر (محجوزة وغير محجوزة)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (slots, error) = await _service.GetAllSlotsAsync(GetUserId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(slots);
    }

    /// <summary>
    /// إضافة موعد مسح جديد
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Add([FromForm] UpsertScanSlotDto dto)
    {
        var (slot, error) = await _service.AddSlotAsync(GetUserId(), dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetAll), slot);
    }

    /// <summary>
    /// تعديل موعد مسح — لا يُسمح إذا كان محجوزاً
    /// </summary>
    [HttpPut("{slotId}")]
    public async Task<IActionResult> Update(int slotId, [FromForm] UpsertScanSlotDto dto)
    {
        var (slot, error) = await _service.UpdateSlotAsync(GetUserId(), slotId, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(slot);
    }

    /// <summary>
    /// حذف موعد مسح — لا يُسمح إذا كان محجوزاً
    /// </summary>
    [HttpDelete("{slotId}")]
    public async Task<IActionResult> Delete(int slotId)
    {
        var error = await _service.DeleteSlotAsync(GetUserId(), slotId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { message = "تم حذف الموعد بنجاح." });
    }

    /// <summary>
    /// عرض الحجوزات التي أجراها الدكاترة على مواعيد هذا المخبر
    /// </summary>
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings()
    {
        var (bookings, error) = await _service.GetBookingsAsync(GetUserId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(bookings);
    }

    /// <summary>
    /// عدد المواعيد المحجوزة
    /// </summary>
    [HttpGet("bookings/count")]
    public async Task<IActionResult> GetBookingsCount()
    {
        var (count, error) = await _service.GetBookingsCountAsync(GetUserId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { bookingsCount = count });
    }

    // ─── helper ────────────────────────────────────────────────────────────────

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException("المستخدم غير مصرح.");
        return int.Parse(claim.Value);
    }
}
