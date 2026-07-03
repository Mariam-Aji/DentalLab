using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LabVerificationController : ControllerBase
    {
        private readonly ILabVerificationService _verificationService;

        public LabVerificationController(ILabVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpGet("pending-accounts")]
        public async Task<IActionResult> GetPendingLabs()
        {
            var pendingLabs = await _verificationService.GetPendingLabsOnlyAsync();

            if (pendingLabs == null || !pendingLabs.Any())
            {
                return Ok(new { message = "لا يوجد حسابات مخابر معلقة حالياً." });
            }

            return Ok(pendingLabs);
        }
        [HttpPut("{userId}/suspend")]
        public async Task<IActionResult> SuspendLab(int userId)
        {
            var (success, message) = await _verificationService.SuspendLabAccountAsync(userId);

            if (!success)
            {
                return BadRequest(new { message = message });
            }

            return Ok(new { message = message });
        }
    }
}