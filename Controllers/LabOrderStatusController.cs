using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab-order-status")]
[Authorize(Roles = "Lab")]
public class LabOrderStatusController : ControllerBase
{
    private readonly ILabOrderStatusService _service;
    private readonly ILabProfileService _labProfileService;
    private readonly IWebHostEnvironment _env;

    public LabOrderStatusController(
        ILabOrderStatusService service,
        ILabProfileService labProfileService,
        IWebHostEnvironment env)
    {
        _service          = service;
        _labProfileService = labProfileService;
        _env              = env;
    }

    // ================================================================
    // GET api/lab-order-status/counts
    // عدد الطلبات لكل حالة يمر فيها الطلب
    // Response: [ { status: "Pending", count: 5 }, { status: "Accepted", count: 3 }, ... ]
    // ================================================================
    [HttpGet("counts")]
    public async Task<IActionResult> GetOrdersCountByStatus()
    {
        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var counts = await _service.GetOrdersCountByStatusAsync(lab.Id);
        return Ok(counts);
    }

    // ================================================================
    // GET api/lab-order-status/by-status/{status}
    // عرض الطلبيات بالتفصيل حسب الحالة
    // مثال: GET api/lab-order-status/by-status/InDesign
    // الحالات المتاحة: Pennding, Accepted, RequestInfo, InDesign,
    //                  InProduction, WaitingForClarification,
    //                  Ready, Delivered, Cancelled
    // ================================================================
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetOrdersByStatus(string status)
    {
        if (!Enum.TryParse<CaseStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return BadRequest(new
            {
                message     = "الحالة المدخلة غير صحيحة.",
                validValues = Enum.GetNames<CaseStatus>()
            });
        }

        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (orders, error) = await _service.GetOrdersByStatusAsync(lab.Id, parsedStatus);
        if (error != null) return BadRequest(new { message = error });

        return Ok(orders);
    }

    // ================================================================
    // GET api/lab-order-status/{orderId}
    // جلب تفاصيل طلبية واحدة تخص المخبر
    // ================================================================
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderById(int orderId)
    {
        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (order, error) = await _service.GetOrderByIdAsync(orderId, lab.Id);
        if (error != null) return NotFound(new { message = error });

        return Ok(order);
    }

    // ================================================================
    // PUT api/lab-order-status/{orderId}
    // تحديث حالة الطلب مع إمكانية رفع صورة النتيجة
    //
    // Form fields:
    //   Status      (string, مطلوب)  - مثل: InProduction, Ready, Delivered
    //   Notes       (string, اختياري)
    //   ResultImage (file,   اختياري) - jpg / jpeg / png / webp
    // ================================================================
    [HttpPut("{orderId}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromForm] UpdateOrderStatusDto dto)
    {
        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (result, error) = await _service.UpdateOrderStatusAsync(
            orderId,
            lab.Id,
            dto,
            _env.ContentRootPath);

        if (error != null) return BadRequest(new { message = error });

        return Ok(result);
    }

    // ---- helpers ----
    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("المستخدم غير مصرح.");
        return int.Parse(claim.Value);
    }

    private async Task<Lab?> GetLabAsync()
    {
        return await _labProfileService.GetProfileAsync(GetUserId());
    }
}
