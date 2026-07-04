using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize(Roles = "Lab")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILabProfileService _labProfileService;

    public CalendarController(ICalendarService calendarService, ILabProfileService labProfileService)
    {
        _calendarService = calendarService;
        _labProfileService = labProfileService;
    }

    /// <summary>
    /// GET api/calendar/monthly?year=2024&amp;month=5
    /// يرجع ملخص التقويم الشهري: كل يوم فيه بيانات مع عدد الطلبات وعدد مواعيد المسح
    /// </summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2000 || year > 2100)
            return BadRequest(new { message = "السنة غير صالحة." });

        if (month < 1 || month > 12)
            return BadRequest(new { message = "الشهر يجب أن يكون بين 1 و 12." });

        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null)
            return NotFound(new { message = "المختبر غير موجود." });

        var result = await _calendarService.GetMonthlyCalendarAsync(lab.Id, year, month);
        return Ok(result);
    }

    /// <summary>
    /// GET api/calendar/day?date=2024-05-20
    /// يرجع تفاصيل يوم محدد: الطلبات ومواعيد المسح بالكامل
    /// </summary>
    [HttpGet("day")]
    public async Task<IActionResult> GetDay([FromQuery] DateOnly date)
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null)
            return NotFound(new { message = "المختبر غير موجود." });

        var result = await _calendarService.GetDayDetailAsync(lab.Id, date);
        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException("المستخدم غير مصرح.");
        return int.Parse(claim.Value);
    }
}
