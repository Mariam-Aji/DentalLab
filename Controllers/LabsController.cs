using System.Security.Claims;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabsController : ControllerBase
    {
        private readonly ILabService _labService;

        public LabsController(ILabService labService)
        {
            _labService = labService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLabs()
        {
            int? currentDentistId = GetCurrentUserIdOrDefault();
            var result = await _labService.GetLabsSummaryAsync(currentDentistId);
            return Ok(result);
        }

        [HttpGet("connected")]
        public async Task<IActionResult> GetConnectedLabs()
        {
            int? currentDentistId = GetCurrentUserIdOrDefault();
            var result = await _labService.GetConnectedLabsAsync(currentDentistId);
            return Ok(result);
        }

        [HttpGet("disconnected")]
        public async Task<IActionResult> GetDisconnectedLabs()
        {
            int? currentDentistId = GetCurrentUserIdOrDefault();
            var result = await _labService.GetDisconnectedLabsAsync(currentDentistId);
            return Ok(result);
        }

        private int? GetCurrentUserIdOrDefault()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllLabsForAdmin()
        {
            var labs = await _labService.GetAllLabsForAdminAsync();

            if (labs == null || !labs.Any())
            {
                return Ok(new
                {
                    success = true,
                    message = "لا توجد مخابر مسجلة حالياً.",
                    count = 0,
                    data = new List<object>()
                });
            }

            return Ok(new
            {
                success = true,
                message = "تم جلب المخابر بنجاح.",
                count = labs.Count,
                data = labs
            });
        }
    
}
}