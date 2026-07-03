using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] 
    public class LabSubscriptionController : ControllerBase
    {
        private readonly ILabSubscriptionService _subscriptionService;

        public LabSubscriptionController(ILabSubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost("lab/{labId}/activate-subscription")]
        public async Task<IActionResult> ActivateSubscription(int labId, [FromForm] CreateSubscriptionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message) = await _subscriptionService.CreateLabSubscriptionAsync(labId, dto);

            if (!success)
            {
                return BadRequest(new { message = message });
            }

            return Ok(new { message = message });
        }
        [HttpGet("active-subscribed-labs")]
        public async Task<IActionResult> GetActiveSubscribedLabs()
        {
            var activeLabs = await _subscriptionService.GetActiveSubscribedLabsAsync();

            if (activeLabs == null || !activeLabs.Any())
            {
                return Ok(new { message = "لا يوجد أي مخابر مشتركة بنشاط في الوقت الحالي." });
            }

            return Ok(activeLabs);
        }
    
        [HttpPut("{labId}/update-info")]
        public async Task<IActionResult> UpdateSubscriptionInfo(int labId, [FromForm] UpdateSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _subscriptionService.UpdateSubscriptionInfoAsync(labId, dto);

            if (!result.Success)
                return NotFound(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        [HttpPost("{labId}/renew")]
        public async Task<IActionResult> RenewSubscription(int labId, [FromForm] RenewSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _subscriptionService.RenewSubscriptionAsync(labId, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
    }
}