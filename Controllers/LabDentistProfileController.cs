using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab/dentists")]
[Authorize(Roles = "Lab")]
public class LabDentistProfileController : ControllerBase
{
    private readonly ILabDentistProfileService _service;

    public LabDentistProfileController(ILabDentistProfileService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET api/lab/dentists/{dentistId}
    /// تفاصيل طبيب محدد — يُستخدم عند الاطلاع على تفاصيل أكثر من قائمة الشكاوى أو الطلبيات
    /// </summary>
    [HttpGet("{dentistId:int}")]
    public async Task<IActionResult> GetDentistProfile(int dentistId)
    {
        var (result, error) = await _service.GetDentistProfileAsync(dentistId);

        if (error != null)
            return NotFound(new { message = error });

        return Ok(result);
    }
}
