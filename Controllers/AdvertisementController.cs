using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvertisementController : ControllerBase
{
    private readonly IAdvertisementService _advService;

    public AdvertisementController(IAdvertisementService advService)
    {
        _advService = advService;
    }

    [HttpPost("admin/create-ads-client")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateADSClient([FromForm] CreateADSClientDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (user, error) = await _advService.CreateADSClientByAdminAsync(dto);

            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            return Ok(new
            {
                message = "تم إنشاء حساب عميل الإعلانات بنجاح.",
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Phone,
                    user.NamePlace,
                    Role = user.Role.ToString(),
                    Status = user.Status.ToString(),
                    user.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة طلب الأدمن.", error = ex.Message });
        }
    }

    [HttpPost("user/{userId}/advertisement")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateAdvertisement([FromRoute] int userId, [FromForm] CreateAdvertisementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (advertisement, error) = await _advService.CreateAdvertisementAsync(userId, dto);

            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            var responseImages = !string.IsNullOrEmpty(advertisement.ImageUrl)
                ? advertisement.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return Ok(new
            {
                message = "تم إضافة الإعلان ورفع كافة الصور بنجاح.",
                advertisement = new
                {
                    advertisement.Id,
                    advertisement.Title,
                    advertisement.Content,
                    advertisement.UserId,
                    advertisement.IsActive,
                    advertisement.CreatedAt,
                    advertisement.ExpiresAt,
                    Images = responseImages
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة إضافة الإعلان.", error = ex.Message });
        }
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAdvertisementsForAdmin()
    {
        try
        {
            var advertisements = await _advService.GetAllAdvertisementsForAdminAsync();

            var result = advertisements.Select(adv => new
            {
                adv.Id,
                adv.Title,
                adv.Content,
                adv.UserId,
                adv.IsActive,
                adv.CreatedAt,
                adv.ExpiresAt,
                Images = !string.IsNullOrEmpty(adv.ImageUrl)
                    ? adv.ImageUrl.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء جلب الإعلانات.", error = ex.Message });
        }
    }

    [HttpGet("target-audiences")]
    [AllowAnonymous]
    public IActionResult GetTargetAudiences()
    {
        var audiences = Enum.GetValues(typeof(TargetAudience))
            .Cast<TargetAudience>()
            .Select(t => new
            {
                Id = (int)t,
                Name = t.ToString(),
                DisplayName = t switch
                {
                    TargetAudience.Dentists => "أطباء الأسنان فقط",
                    TargetAudience.Labs => "مخابر الأسنان فقط",
                    TargetAudience.Both => "الأطباء والمخابر معاً",
                    _ => t.ToString()
                }
            })
            .ToList();

        return Ok(audiences);
    }

    [HttpPut("admin/update/{advId}")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateAdvertisement([FromRoute] int advId, [FromForm] UpdateAdvertisementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (advertisement, error) = await _advService.UpdateAdvertisementAsync(advId, dto);
            if (error != null) return BadRequest(new { message = error });

            var responseImages = !string.IsNullOrEmpty(advertisement!.ImageUrl)
                ? advertisement.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return Ok(new
            {
                message = "تم تحديث الإعلان بنجاح.",
                advertisement = new
                {
                    advertisement.Id,
                    advertisement.Title,
                    advertisement.Content,
                    advertisement.UserId,
                    advertisement.IsActive,
                    advertisement.CreatedAt,
                    advertisement.ExpiresAt,
                    Images = responseImages
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة تعديل الإعلان.", error = ex.Message });
        }
    }

    [HttpDelete("admin/delete/{advId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAdvertisement([FromRoute] int advId)
    {
        var (success, error) = await _advService.DeleteAdvertisementAsync(advId);
        if (!success) return BadRequest(new { message = error });

        return Ok(new { message = "تم حذف الإعلان وإزالة كافة ملفاته المرفوعة بنجاح." });
    }

    [HttpPatch("admin/toggle-status/{advId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleAdStatus([FromRoute] int advId)
    {
        var (advertisement, error) = await _advService.ToggleAdStatusAsync(advId);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new
        {
            message = "تم تغيير حالة تفعيل الإعلان بنجاح.",
            id = advertisement!.Id,
            isActive = advertisement.IsActive
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("dentists")]
    public async Task<IActionResult> GetAdvertisementsForDentists()
    {
        try
        {
            var advertisements = await _advService.GetAdvertisementsForDentistsAsync();
            var result = MapAdvertisementsToResponse(advertisements);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء جلب إعلانات أطباء الأسنان.", error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Dentist")]
    [HttpGet("labs")]
    public async Task<IActionResult> GetAdvertisementsForLabs()
    {
        try
        {
            var advertisements = await _advService.GetAdvertisementsForLabsAsync();
            var result = MapAdvertisementsToResponse(advertisements);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء جلب إعلانات مخابر الأسنان.", error = ex.Message });
        }
    }

    private List<object> MapAdvertisementsToResponse(List<Advertisement> advertisements)
    {
        return advertisements.Select(adv => new
        {
            adv.Id,
            adv.Title,
            adv.Content,
            adv.UserId,
            adv.IsActive,
            adv.CreatedAt,
            adv.ExpiresAt,
            Images = !string.IsNullOrEmpty(adv.ImageUrl)
                ? adv.ImageUrl.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>()
        }).Cast<object>().ToList();
    }

    [HttpPost("doctor/create-advertisement")]
    [Authorize(Roles = "Dentist")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateAdvertisementByDoctor([FromForm] CreateAdvertisementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            int doctorId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);

            var (advertisement, error) = await _advService.CreateAdvertisementByDoctorAsync(doctorId, dto);

            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            var responseImages = !string.IsNullOrEmpty(advertisement!.ImageUrl)
                ? advertisement.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return Ok(new
            {
                message = "تم تقديم طلب الإعلان بنجاح، وتم إرسال محتواه بالكامل إلى الأدمن للمراجعة والتفعيل.",
                advertisement = new
                {
                    advertisement.Id,
                    advertisement.Title,
                    advertisement.Content,
                    advertisement.UserId,
                    advertisement.IsActive,
                    advertisement.CreatedAt,
                    advertisement.ExpiresAt,
                    Images = responseImages
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة طلب الإعلان من قبل الطبيب.", error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("admin/accept-and-publish/user/{userId}/advertisement/{id}")]
    public async Task<IActionResult> AcceptAndPublish([FromRoute] int userId, [FromRoute] int id, [FromForm] decimal price)
    {
        try
        {
            var (isProcessed, errorMessage) = await _advService.ActivateDoctorAdvertisementAsync(id, userId, price);

            if (!isProcessed)
            {
                return BadRequest(new { message = errorMessage });
            }

            var advertisements = await _advService.GetAllAdvertisementsForAdminAsync();
            var advertisement = advertisements.FirstOrDefault(a => a.Id == id);

            if (advertisement == null)
            {
                return NotFound(new { message = "تعذر العثور على بيانات الإعلان بعد المعالجة." });
            }

            var responseImages = !string.IsNullOrEmpty(advertisement.ImageUrl)
                ? advertisement.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return Ok(new
            {
                message = $"تم قبول الإعلان مبدئياً وإرسال طلب دفع بمبلغ {price} إلى الطبيب بنجاح. يبقى الإعلان معلقاً حتى إتمام الدفع.",
                advertisement = new
                {
                    advertisement.Id,
                    advertisement.Title,
                    advertisement.Content,
                    advertisement.UserId,
                    advertisement.IsActive,
                    advertisement.CreatedAt,
                    advertisement.ExpiresAt,
                    Images = responseImages
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة اعتماد الإعلان وتحديد القيمة المالية.", error = ex.Message });
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _advService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"حدث خطأ أثناء جلب المستخدمين: {ex.Message}");
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto dto)
    {
        var (result, error) = await _advService.UpdateUserAsync(id, dto);

        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var (success, error) = await _advService.DeleteUserAsync(id);

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "تم حذف المستخدم بنجاح." });
    }
    [Authorize(Roles = "Admin")]

    [HttpGet("user/{userId}/valid-advertisements")]
    public async Task<IActionResult> GetUserValidAdvertisements([FromRoute] int userId)
    {
        try
        {
            var (data, error) = await _advService.GetUserActiveAdvertisementsWithCountAsync(userId);

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "حدث خطأ داخلي أثناء معالجة طلب إعلانات المستخدم.",
                error = ex.Message
            });
        }
    }
    [Authorize(Roles = "Admin")]

    [HttpPost("search")]
    public async Task<IActionResult> SearchAdvertisements([FromForm] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "يرجى إدخال كلمة مفتاحية للبحث." });
            }

            var (data, error) = await _advService.SearchAdvertisementsServiceAsync(query);

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "حدث خطأ داخلي أثناء عملية البحث.",
                error = ex.Message
            });
        }
    }

}