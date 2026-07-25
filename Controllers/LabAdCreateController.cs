using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

/// <summary>
/// إدارة إعلانات المخبر (إنشاء + دفع)
/// الدفع عبر: POST /api/Advertisement/pay-advertisement/{adId}
/// قبول الأدمن عبر: PATCH /api/Advertisement/admin/accept-and-publish/user/{userId}/advertisement/{id}
/// </summary>
[ApiController]
[Route("api/lab-ad")]
[Authorize(Roles = "Lab")]
public class LabAdCreateController : ControllerBase
{
    private readonly ILabAdCreateService _service;

    public LabAdCreateController(ILabAdCreateService service)
    {
        _service = service;
    }

    // POST api/lab-ad/create
    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateAdvertisementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var labUserId = GetUserId();
            var (result, error) = await _service.CreateLabAdvertisementAsync(labUserId, dto);

            if (error != null) return BadRequest(new { message = error });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي.", error = ex.Message });
        }
    }

    // GET api/lab-ad/pending-payment  — وافق عليها الأدمن وبانتظار الدفع
    [HttpGet("pending-payment")]
    public async Task<IActionResult> GetPendingPayment()
    {
        var ads = await _service.GetPendingPaymentAdsAsync(GetUserId());
        return Ok(new { count = ads.Count, data = ads });
    }

    // GET api/lab-ad/active  — نشطة ومدفوعة
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var ads = await _service.GetActiveAdsAsync(GetUserId());
        return Ok(new { count = ads.Count, data = ads });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException());
}
