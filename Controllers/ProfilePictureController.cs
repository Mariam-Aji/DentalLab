using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

/// <summary>
/// Dedicated endpoints for managing a lab user's profile picture.
/// Intentionally separated from profile endpoints.
/// </summary>
[ApiController]
[Route("api/profile-picture-lab")]
[Authorize(Roles = "Lab")]
public class ProfilePictureController : ControllerBase
{
    private readonly IProfilePictureService _profilePictureService;

    public ProfilePictureController(IProfilePictureService profilePictureService)
    {
        _profilePictureService = profilePictureService;
    }

    // GET api/profile-picture
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        var url = await _profilePictureService.GetProfilePictureAsync(userId);

        if (url == null)
            return Ok(new { profilePictureUrl = (string?)null, message = "No profile picture set." });

        return Ok(new { profilePictureUrl = url });
    }

    // POST api/profile-picture-lab   (upload or replace)
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto dto)
    {
        var userId = GetUserId();
        var (url, error) = await _profilePictureService.UploadProfilePictureAsync(userId, dto.File);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(new { profilePictureUrl = url, message = "Profile picture updated successfully." });
    }

    // DELETE api/profile-picture
    [HttpDelete]
    public async Task<IActionResult> DeletePictuer()
    {
        var userId = GetUserId();
        var error = await _profilePictureService.DeleteProfilePictureAsync(userId);

        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Profile picture deleted successfully." });
    }

    // -------------------------------------------------------
    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found.");
        return int.Parse(claim.Value);
    }
}
