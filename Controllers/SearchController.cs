using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DentalLab.Api.Services;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// البحث عن مخبر محدد مع جلب بياناته وصورة البروفايل وحالة الاتصال والتوفر والارتباط (متاح للزوار والأعضاء)
    /// </summary>
    [HttpGet("labs")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchLabs([FromForm] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "يرجى إدخال اسم المخبر المراد البحث عنه." });
        }

        try
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                currentUserId = parsedId;
            }

            var result = await _searchService.SearchLabsByNameAsync(name, currentUserId);

            if (result.Count == 0)
            {
                return NotFound(new { message = "لم يتم العثور على أي مخبر مطابق للبحث." });
            }

            return Ok(new
            {
                count = result.Count,
                isUserLoggedIn = currentUserId.HasValue,
                labs = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ أثناء عملية البحث عن المخبر.", error = ex.Message });
        }
    }

    
    [HttpGet("blog")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchBlog([FromForm] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "يرجى إدخال كلمة مفتاحية للبحث في المدونة." });
        }

        try
        {
            var result = await _searchService.SearchBlogPostsAsync(query);

            if (result.Count == 0)
            {
                return NotFound(new { message = "لم يتم العثور على أي نتائج مطابقة في المدونة." });
            }

            return Ok(new
            {
                count = result.Count,
                posts = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ أثناء البحث في المدونة.", error = ex.Message });
        }
    }
}