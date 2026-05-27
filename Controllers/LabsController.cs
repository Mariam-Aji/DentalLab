using DentalLab.Api.Services;
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
            var result = await _labService.GetLabsSummaryAsync();
            return Ok(result);
        }

        [HttpGet("connected")]
        public async Task<IActionResult> GetConnectedLabs()
        {
            var result = await _labService.GetConnectedLabsAsync();
            return Ok(result);
        }

        [HttpGet("disconnected")]
        public async Task<IActionResult> GetDisconnectedLabs()
        {
            var result = await _labService.GetDisconnectedLabsAsync();
            return Ok(result);
        }
        //
    }
}