using System.Security.Claims;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("ordered-by-rating")]
        public async Task<IActionResult> GetOrderedLabs()
        {
            int? currentUserId = GetUserIdOrDefault();
            var result = await _ratingService.GetTopRatedLabsAsync(currentUserId);
            return Ok(result);
        }

        [HttpGet("filter-by-my-location")]
        [Authorize(Roles = "Dentist")]
        public async Task<IActionResult> GetLabsInMyLocation()
        {
            var doctorId = GetUserId();
            var result = await _ratingService.GetLabsByDoctorLocationAsync(doctorId);
            return Ok(result);
        }

        [HttpGet("lab-profile/{labId}")]
        public async Task<IActionResult> GetLabProfile(int labId)
        {
            int? currentUserId = GetUserIdOrDefault();
            var details = await _ratingService.GetLabProfileDetailsAsync(labId, currentUserId);

            if (details == null)
                return NotFound(new { Message = "المخبر غير موجود" });

            return Ok(details);
        }

        [HttpGet("with-scan-service")]
        public async Task<IActionResult> GetLabsWithScanService()
        {
            int? currentUserId = GetUserIdOrDefault();
            var labs = await _ratingService.GetLabsWithScanServiceAsync(currentUserId);
            return Ok(labs);
        }

        [HttpGet("available-labs")]
        public async Task<IActionResult> GetAvailableLabs()
        {
            int? currentUserId = GetUserIdOrDefault();
            var labs = await _ratingService.GetAvailableLabsAsync(currentUserId);
            return Ok(labs);
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private int? GetUserIdOrDefault()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("chart-data")]
        public async Task<IActionResult> GetLabsRatingChart()
        {
            var chartData = await _ratingService.GetLabsRatingChartDataAsync();

            return Ok(new
            {
                count = chartData.Count,
                data = chartData
            });
        } }
    }