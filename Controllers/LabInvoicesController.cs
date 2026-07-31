using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab/invoices")]
[Authorize(Roles = "Lab")]
public class LabInvoicesController : ControllerBase
{
    private readonly ILabInvoiceService _service;

    public LabInvoicesController(ILabInvoiceService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/lab/invoices/paid
    /// فواتير الطلبيات والإعلانات المدفوعة للمخبر — مرتبة من الأحدث للأقدم
    /// </summary>
    [HttpGet("paid")]
    public async Task<IActionResult> GetPaidInvoices()
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetPaidInvoicesAsync(userId);

        if (error != null)
            return NotFound(new { message = error });

        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException();
        return int.Parse(claim.Value);
    }
}
