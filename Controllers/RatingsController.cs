using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;
        public RatingsController(IRatingService ratingService) => _ratingService = ratingService;
        [Authorize(Roles = "Dentist")]

        [HttpPost("{labId}/quality/{score}")]
        public async Task<IActionResult> RateQuality(int labId, int score)
        {
            var userId = GetUserId();
            return Ok(await _ratingService.ProcessQualityRating(userId, labId, score));
        }
        [Authorize(Roles = "Dentist")]

        [HttpPost("{labId}/time/{score}")]
        public async Task<IActionResult> RateTime(int labId, int score)
        {
            var userId = GetUserId();
            return Ok(await _ratingService.ProcessTimeRating(userId, labId, score));
        }
        [Authorize(Roles = "Dentist")]
        [HttpPost("{labId}/finalize/{qualityScore}/{timeScore}")]
        public async Task<IActionResult> Finalize(int labId, int qualityScore, int timeScore)
        {
            var userId = GetUserId();
            return Ok(await _ratingService.CalculateAndSaveFinalRatingAsync(userId, labId, timeScore, qualityScore));
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        [HttpGet("ordered-by-rating")]
        public async Task<IActionResult> GetOrderedLabs()
        {
            var result = await _ratingService.GetTopRatedLabsAsync();
            return Ok(result);
        }
        [HttpGet("filter-by-my-location")]
        [Authorize(Roles = "Dentist")]
        public async Task<IActionResult> GetLabsInMyLocation()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int doctorId = int.Parse(userIdClaim.Value);

            var result = await _ratingService.GetLabsByDoctorLocationAsync(doctorId);

            return Ok(result);
        }
        [HttpGet("lab-profile/{labId}")]
        public async Task<IActionResult> GetLabProfile(int labId)
        {
            var details = await _ratingService.GetLabProfileDetailsAsync(labId);

            if (details == null)
                return NotFound(new { Message = "المخبر غير موجود" });

            return Ok(details);
        }
        [HttpGet("with-scan-service")]
        public async Task<IActionResult> GetLabsWithScanService()
        {
            var labs = await _ratingService.GetLabsWithScanServiceAsync();
            return Ok(labs);
        }
    }
    //





}