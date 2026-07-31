using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab-advertisements")]
[Authorize(Roles = "Lab")]
public class LabAdvertisementController : ControllerBase
{
    private readonly ILabAdvertisementService _service;

    public LabAdvertisementController(ILabAdvertisementService service)
    {
        _service = service;
    }

    // GET api/lab-advertisements
    [HttpGet]
    public async Task<IActionResult> GetAdsForLab()
    {
        try
        {
            var ads = await _service.GetAdsForLabAsync();
            return Ok(new { count = ads.Count, data = ads });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء جلب الإعلانات.", error = ex.Message });
        }
    }
}
