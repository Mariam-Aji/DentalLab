using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab-orders")]
[Authorize(Roles = "Lab")]
public class LabOrderQuoteController : ControllerBase
{
    private readonly ILabOrderQuoteService _quoteService;
    private readonly ILabProfileService    _labProfileService;

    public LabOrderQuoteController(
        ILabOrderQuoteService quoteService,
        ILabProfileService labProfileService)
    {
        _quoteService      = quoteService;
        _labProfileService = labProfileService;
    }

    /// <summary>
    /// عرض السعر (فاتورة تقديرية) لطلبية معينة —
    /// مجمّع حسب نوع التعويض مع الكمية وسعر الوحدة والمجموع
    /// </summary>
    [HttpGet("{orderId}/quote")]
    public async Task<IActionResult> GetOrderQuote(int orderId)
    {
        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (quote, error) = await _quoteService.GetOrderQuoteAsync(orderId, lab.Id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(quote);
    }

    /// <summary>
    /// المخبر يدخل السعر النهائي للطلبية
    /// </summary>
    [HttpPost("{orderId}/quote/final-price")]
    public async Task<IActionResult> SetFinalPrice(int orderId, [FromForm] SetFinalPriceDto dto)
    {
        var lab = await GetLabAsync();
        if (lab == null) return NotFound(new { message = "المخبر غير موجود." });

        var (result, error) = await _quoteService.SetFinalPriceAsync(orderId, lab.Id, dto.FinalPrice);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async Task<DentalLab.Api.Models.Lab?> GetLabAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) return null;
        return await _labProfileService.GetProfileAsync(int.Parse(claim.Value));
    }
}
