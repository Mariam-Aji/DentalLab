using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab/complaints")]
[Authorize(Roles = "Lab")]
public class LabComplaintsController : ControllerBase
{
    private readonly ILabComplaintService _service;

    public LabComplaintsController(ILabComplaintService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/lab/complaints
    /// عرض جميع الشكاوى الواردة للمخبر مع الرد إن وُجد
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyComplaints()
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetLabComplaintsAsync(userId);

        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// GET api/lab/complaints/{complaintId}
    /// تفاصيل شكوى محددة
    /// </summary>
    [HttpGet("{complaintId:int}")]
    public async Task<IActionResult> GetComplaintById(int complaintId)
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetComplaintByIdAsync(userId, complaintId);

        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// POST api/lab/complaints/{complaintId}/reply
    /// رد المخبر على شكوى — يُرسل إشعار للطبيب فور الرد
    /// </summary>
    [HttpPost("{complaintId:int}/reply")]
    public async Task<IActionResult> ReplyToComplaint(int complaintId, [FromForm] LabComplaintReplyDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var (result, error) = await _service.ReplyToComplaintAsync(userId, complaintId, dto);

        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException();
        return int.Parse(claim.Value);
    }
}
