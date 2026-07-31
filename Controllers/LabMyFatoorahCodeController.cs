using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

/// <summary>
/// إدارة كود حساب MyFatoorah الخاص بالمخبر
/// يُستخدم هذا الكود لاستقبال المدفوعات من الأطباء عبر MyFatoorah
/// </summary>
[ApiController]
[Route("api/lab/myfatoorah-code")]
[Authorize(Roles = "Lab")]
public class LabMyFatoorahCodeController : ControllerBase
{
    private readonly ILabMyFatoorahCodeService _service;

    public LabMyFatoorahCodeController(ILabMyFatoorahCodeService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/lab/myfatoorah-code
    /// جلب كود الحساب الحالي على MyFatoorah
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyCode()
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetCodeAsync(userId);

        if (error != null)
            return NotFound(new { message = error });

        return Ok(result);
    }

    /// <summary>
    /// PUT api/lab/myfatoorah-code
    /// تعديل كود الحساب على MyFatoorah (أو إرسال null لحذفه)
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateMyCode([FromForm] UpdateLabMyFatoorahCodeDto dto)
    {
        var userId = GetUserId();
        var (result, error) = await _service.UpdateCodeAsync(userId, dto);

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
