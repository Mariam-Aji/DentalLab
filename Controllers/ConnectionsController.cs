using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Dentist")]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    public ConnectionsController(IConnectionService connectionService) => _connectionService = connectionService;

    [HttpPost("follow/{labId}")]
    public async Task<IActionResult> FollowLab(int labId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value!;

        var result = await _connectionService.SendFollowRequestAsync(userId, userRole, labId);

        if (result.Contains("بنجاح")) return Ok(new { message = result });
        if (result.Contains("غير موجود")) return NotFound(new { message = result });

        return BadRequest(new { message = result });
    }
}