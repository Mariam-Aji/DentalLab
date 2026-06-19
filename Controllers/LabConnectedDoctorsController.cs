using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab-connected-doctors")]
[Authorize(Roles = "Lab")]
public class LabConnectedDoctorsController : ControllerBase
{
    private readonly ILabConnectedDoctorsService _service;

    public LabConnectedDoctorsController(ILabConnectedDoctorsService service)
    {
        _service = service;
    }

    // ================================================================
    // GET api/lab-connected-doctors
    // قائمة الأطباء المتصلين بالمخبر
    // ================================================================
    [HttpGet]
    public async Task<IActionResult> GetConnectedDoctors()
    {
        var (doctors, error) = await _service.GetConnectedDoctorsAsync(GetUserId());
        if (error != null) return NotFound(new { message = error });
        return Ok(doctors);
    }

    // ================================================================
    // GET api/lab-connected-doctors/{dentistId}/orders
    // طلبيات طبيب محدد تخص هذا المخبر
    // ================================================================
    [HttpGet("{dentistId:int}/orders")]
    public async Task<IActionResult> GetOrdersByDentist(int dentistId)
    {
        var (orders, error) = await _service.GetOrdersByDentistAsync(GetUserId(), dentistId);
        if (error != null) return NotFound(new { message = error });
        return Ok(orders);
    }

    // ================================================================
    // DELETE api/lab-connected-doctors/{dentistId}
    // قطع اتصال المخبر مع طبيب محدد
    // ================================================================
    [HttpDelete("{dentistId:int}")]
    public async Task<IActionResult> DisconnectDoctor(int dentistId)
    {
        var error = await _service.DisconnectDoctorAsync(GetUserId(), dentistId);
        if (error != null) return NotFound(new { message = error });
        return Ok(new { message = "تم قطع الاتصال مع الطبيب بنجاح." });
    }

    // ---- helper ----
    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException());
}
