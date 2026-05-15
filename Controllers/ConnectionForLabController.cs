using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Lab")]
public class ConnectionForLabController : ControllerBase
{
    private readonly IConnectionForLabService _connectionForLabService;

    public ConnectionForLabController(IConnectionForLabService connectionForLabService)
    {
        _connectionForLabService = connectionForLabService;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var (requests, error) = await _connectionForLabService.GetPendingRequestsForLabAsync(userId);

        if (error != null) return NotFound(new { message = error });

        var response = requests.Select(r => new
        {
            r.Id,
            r.Status,
            r.CreatedAt,
            Dentist = new
            {
                r.FromDentist.Id,
                r.FromDentist.Name,
                r.FromDentist.Phone,
                r.FromDentist.NamePlace,
                r.FromDentist.AddressPlace,
                r.FromDentist.CityPlace,
                r.FromDentist.CountryPlace,
            }
        });

        return Ok(response);
    }

    [HttpPost("requests/{id:int}/accept")]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var error = await _connectionForLabService.AcceptRequestAsync(userId, id);
        if (error != null)
        {
            if (error.Contains("غير موجود")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "تمت الموافقة على الطلب." });
    }

    [HttpPost("requests/{id:int}/reject")]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var error = await _connectionForLabService.RejectRequestAsync(userId, id);
        if (error != null)
        {
            if (error.Contains("غير موجود")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "تم رفض الطلب." });
    }
}
