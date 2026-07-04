using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/lab-blog")]
public class LabBlogController : ControllerBase
{
    private readonly ILabBlogService _labBlogService;

    public LabBlogController(ILabBlogService labBlogService)
    {
        _labBlogService = labBlogService;
    }

    [HttpPost("create")]
    [Authorize(Roles = "Lab")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var labId = GetUserId();
            var (result, error) = await _labBlogService.CreateLabPostAsync(dto, labId);
            if (error != null) return BadRequest(new { message = error });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{postId:int}/update")]
    [Authorize(Roles = "Lab")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePost(int postId, [FromForm] UpdatePostDto dto)
    {
        try
        {
            var labId = GetUserId();
            var (result, error) = await _labBlogService.UpdateLabPostAsync(postId, dto, labId);
            if (error != null)
            {
                if (error.Contains("غير موجود", StringComparison.OrdinalIgnoreCase)) return NotFound(new { message = error });
                return BadRequest(new { message = error });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-posts")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> GetMyPosts()
    {
        try
        {
            return Ok(await _labBlogService.GetLabApprovedPostsAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-posts/pending")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> GetMyPendingPosts()
    {
        try
        {
            return Ok(await _labBlogService.GetLabPendingPostsAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-posts/rejected")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> GetMyRejectedPosts()
    {
        try
        {
            return Ok(await _labBlogService.GetLabRejectedPostsAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{postId:int}")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> DeleteMyPost(int postId)
    {
        try
        {
            var (success, error) = await _labBlogService.DeleteLabPostAsync(postId, GetUserId());
            if (!success)
            {
                if (error != null && error.Contains("غير موجود", StringComparison.OrdinalIgnoreCase)) return NotFound(new { message = error });
                return BadRequest(new { message = error });
            }

            return Ok(new { message = "تم حذف المنشور بنجاح." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("all-posts")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> GetAllLabPosts()
    {
        try
        {
            return Ok(await _labBlogService.GetAllApprovedLabPostsAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("doctor-posts")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> GetDoctorPosts()
    {
        try
        {
            return Ok(await _labBlogService.GetApprovedDoctorPostsAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("search")]
    [Authorize(Roles = "Lab")]
    public async Task<IActionResult> SearchBlogPosts([FromForm] string query)
    {
        try
        {
            var (data, error) = await _labBlogService.SearchBlogPostsServiceAsync(query);

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

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException();
        return int.Parse(claim.Value);
    }
}
