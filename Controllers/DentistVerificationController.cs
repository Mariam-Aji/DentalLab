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
    public class DentistVerificationController : ControllerBase
    {
        private readonly IDentistVerificationService _dentistVerificationService;

        public DentistVerificationController(IDentistVerificationService dentistVerificationService)
        {
            _dentistVerificationService = dentistVerificationService;
        }

        [HttpGet("pending-accounts")]
        public async Task<IActionResult> GetPendingDentists()
        {
            var pendingDentists = await _dentistVerificationService.GetPendingDentistsOnlyAsync();

            if (pendingDentists == null || !pendingDentists.Any())
            {
                return Ok(new { message = "لا يوجد حسابات أطباء أسنان معلقة حالياً." });
            }

            return Ok(pendingDentists);
        }
        [HttpPut("{userId}/suspend")]
        public async Task<IActionResult> SuspendDentist(int userId)
        {
            var (success, message) = await _dentistVerificationService.SuspendDentistAccountAsync(userId);

            if (!success)
            {
                return BadRequest(new { message = message });
            }

            return Ok(new { message = message });
        }
    }
}