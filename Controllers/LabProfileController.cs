using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;
//controller
[ApiController]
[Route("api/lab-profile")]
[Authorize(Roles = "Lab")]
public class LabProfileController : ControllerBase
{
    private readonly ILabProfileService _labProfileService;
    private readonly ILabGalleryService _labGalleryService;

    public LabProfileController(ILabProfileService labProfileService, ILabGalleryService labGalleryService)
    {
        _labProfileService = labProfileService;
        _labGalleryService = labGalleryService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound();

        return Ok(MapLabProfile(lab));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromForm] LabProfileUpdateDto dto)
    {
        var userId = GetUserId();
        var (lab, error) = await _labProfileService.UpdateProfileAsync(userId, dto);
        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        if (lab == null) return NotFound();
        return Ok(MapLabProfile(lab));
    }

    [HttpPost("prices")]
    public async Task<IActionResult> AddPrice([FromForm] LabPriceUpsertDto dto)
    {
        var userId = GetUserId();
        var (price, error) = await _labProfileService.AddPriceAsync(userId, dto);
        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        return Ok(price);
    }

    [HttpPut("prices/{id:int}")]
    public async Task<IActionResult> UpdatePrice(int id, [FromForm] LabPriceUpsertDto dto)
    {
        var userId = GetUserId();
        var error = await _labProfileService.UpdatePriceAsync(userId, id, dto);
        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpDelete("prices/{id:int}")]
    public async Task<IActionResult> DeletePrice(int id)
    {
        var userId = GetUserId();
        var error = await _labProfileService.DeletePriceAsync(userId, id);
        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPost("gallery")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddGallery([FromForm] LabGalleryUploadDto dto)
    {
        var userId = GetUserId();
        var lab = await _labProfileService.GetProfileAsync(userId);
        if (lab == null) return NotFound();

        var error = await _labGalleryService.AddLabGalleryAsync(lab.Id, dto.Images);
        if (error != null) return BadRequest(new { message = error });

        return Ok();
    }

    [HttpDelete("gallery/{id:int}")]
    public async Task<IActionResult> DeleteGallery(int id)
    {
        var userId = GetUserId();
        var error = await _labProfileService.DeleteGalleryAsync(userId, id);
        if (error != null)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException();
        return int.Parse(claim.Value);
    }

    private static object MapLabProfile(Models.Lab lab)
    {
        return new
        {
            lab.Id,
            lab.Description,
            lab.YearsOfExperience,
            lab.Specialties,
            lab.Materials,
            lab.Availability,
            lab.HasScanVisitService,
            lab.AverageRating,
            Prices = lab.Prices.Select(p => new
            {
                p.Id,
                p.CompensationType,
                p.UnitPrice,
                p.Notes,
                p.UpdatedAt
            }),
            Gallery = lab.Gallery.Select(g => new
            {
                g.Id,
                g.Path,
                g.Type,
                g.UploadedAt
            }),
            Owner = new
            {
                lab.Owner.Id,
                lab.Owner.Name,
                lab.Owner.Email,
                lab.Owner.Phone,
                lab.Owner.NamePlace,
                lab.Owner.AddressPlace,
                lab.Owner.CityPlace,
                lab.Owner.CountryPlace,
                lab.Owner.Role,
                lab.Owner.Status,
                lab.Owner.CreatedAt
            }
        };
    }
}
