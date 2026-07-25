using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalLab.Api.Dtos;

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
    [HttpPost("upload-profile-picture")]
    [Authorize(Roles = "Dentist")] 
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
       
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new { Message = "جلسة العمل منتهية، يرجى إعادة تسجيل الدخول." });

        int dentistId = int.Parse(userIdClaim.Value);

        
        var (relativePath, error) = await _userService.UpdateProfilePictureAsync(dentistId, file);

        if (error != null)
            return BadRequest(new { Message = error });

        return Ok(new
        {
            Message = "تم تحديث صورة البروفايل بنجاح.",
            ProfilePictureUrl = relativePath
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeUserStatus(int id, [FromForm] ChangeUserStatusDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "بيانات الطلب غير صالحة." });
        }

        var isUpdated = await _userService.ChangeUserStatusAsync(id, dto.Status);

        if (!isUpdated)
        {
            return NotFound(new { message = $"المعالج: لم يتم العثور على المستخدم ذو المعرف {id} أو حدث خطأ أثناء التحديث." });
        }

        return Ok(new { message = "تم تحديث حالة المستخدم بنجاح.", newStatus = dto.Status.ToString() });
    }
}