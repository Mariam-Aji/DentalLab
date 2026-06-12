using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("search")]
    public async Task<IActionResult> SearchUsers([FromForm] string query)
    {
        try
        {
            var (data, error) = await _userService.SearchUsersServiceAsync(query);

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "حدث خطأ داخلي أثناء عملية البحث المعمّق.",
                error = ex.Message
            });
        }
    }
    [Authorize(Roles = "Admin")]

    [HttpGet("dentists")]
    public async Task<IActionResult> GetAllDentists()
    {
        try
        {
            var (data, error) = await _userService.GetAllDentistsServiceAsync();

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "حدث خطأ داخلي أثناء جلب قائمة الأطباء.",
                error = ex.Message
            });
        }
    }
}