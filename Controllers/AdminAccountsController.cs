using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminAccountsController : ControllerBase
{
    private readonly IAdminAccountService _accountService;

    public AdminAccountsController(IAdminAccountService accountService)
        {
            _accountService = accountService;
    }

    [HttpGet("dentists/pending-approval")]
    public async Task<IActionResult> GetPendingDentists()
    {
        var users = await _accountService.GetPendingDentistApprovalsAsync();
        var result = users.Select(user => new
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

        return Ok(result);
    }

    [HttpPut("dentists/{id:int}/approve")]
    public async Task<IActionResult> ApproveDentist(int id)
    {
        var error = await _accountService.ApproveDentistAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Active" });
    }

    [HttpPut("dentists/{id:int}/reject")]
    public async Task<IActionResult> RejectDentist(int id)
    {
        var error = await _accountService.RejectDentistAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Suspended" });
    }

    [HttpPut("dentists/{id:int}/suspend")]
    public async Task<IActionResult> SuspendDentist(int id)
    {
        var error = await _accountService.SuspendDentistAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Suspended" });
    }

    [HttpGet("labs/pending-approval")]
    public async Task<IActionResult> GetPendingLabs()
    {
        var users = await _accountService.GetPendingLabApprovalsAsync();
        var result = users.Select(user => new
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

        return Ok(result);
    }

    [HttpPut("labs/{id:int}/approve")]
    public async Task<IActionResult> ApproveLab(int id)
    {
        var error = await _accountService.ApproveLabAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Active" });
    }

    [HttpPut("labs/{id:int}/reject")]
    public async Task<IActionResult> RejectLab(int id)
    {
        var error = await _accountService.RejectLabAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Suspended" });
    }

    [HttpPut("labs/{id:int}/suspend")]
    public async Task<IActionResult> SuspendLab(int id)
    {
        var error = await _accountService.SuspendLabAsync(id);
        if (error != null) return BadRequest(error);

        return Ok(new { UserId = id, Status = "Suspended" });
    }
}
