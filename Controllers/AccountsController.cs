using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace DentalLab.Api.Controllers;
//controller
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("dentist")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDentist([FromForm] DentistRegistrationDto dto)
    {
        var (result, error) = await _accountService.CreateDentistAsync(dto);
        if (error != null) return Conflict(error);

        return CreatedAtAction(nameof(GetUserById), new { id = result!.UserId }, result);
    }

    [HttpPost("lab")]
    public async Task<IActionResult> CreateLab([FromForm] LabRegistrationDto dto)
    {
        var (result, error) = await _accountService.CreateLabAsync(dto);
        if (error != null) return Conflict(error);

        return CreatedAtAction(nameof(GetLabById), new { id = result!.LabId }, result);
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _accountService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.NamePlace,
            user.AddressPlace,
            user.CityPlace,
            user.CountryPlace,
            user.Role,
            user.Status,
            user.VerificationDocumentPath,
            user.CreatedAt
        });
    }

    [HttpGet("labs/{id:int}")]
    public async Task<IActionResult> GetLabById(int id)
    {
        var lab = await _accountService.GetLabByIdAsync(id);

        if (lab == null) return NotFound();

        return Ok(new
        {
            lab.Id,
            lab.Description,
            lab.YearsOfExperience,
            lab.Specialties,
            lab.Materials,
            lab.Availability,
            lab.HasScanVisitService,
            lab.AverageRating,
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
                lab.Owner.VerificationDocumentPath,
                lab.Owner.CreatedAt
            }
        });
    }

}
