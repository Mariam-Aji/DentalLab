using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Lab")]
public class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService _service;
    private readonly ILabProfileService _labProfileService;

    public LabOrdersController(ILabOrderService service, ILabProfileService labProfileService)
    {
        _service = service;
        _labProfileService = labProfileService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingOrders()
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var orders = await _service.GetPendingOrdersForLabAsync(lab.Id);
        return Ok(orders);
    }

    [HttpGet("pending/count")]
    public async Task<IActionResult> GetPendingOrdersCount()
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var count = await _service.GetPendingOrdersCountForLabAsync(lab.Id);
        return Ok(new { pendingCount = count });
    }

    [HttpPost("{orderId}/approve")]
    public async Task<IActionResult> ApproveOrder(int orderId)
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (result, error) = await _service.ApproveOrderAsync(orderId, lab.Id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    [HttpPost("{orderId}/reject")]
    public async Task<IActionResult> RejectOrder(int orderId, [FromForm] RejectOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "يجب تحديد السبب." });

        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (result, error) = await _service.RejectOrderAsync(orderId, lab.Id, dto.Reason);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    [HttpPost("{orderId}/request-info")]
    public async Task<IActionResult> RequestMoreInfo(int orderId, [FromForm] RequestInfoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "يجب تحديد المعلومات المطلوبة." });

        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (result, error) = await _service.RequestMoreInfoAsync(orderId, lab.Id, dto.Message);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException("المستخدم غير مصرح.");
        return int.Parse(claim.Value);
    }
}

public class RejectOrderDto
{
    public string Reason { get; set; } = "";
}

public class RequestInfoDto
{
    public string Message { get; set; } = "";
}
