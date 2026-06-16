using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public DoctorBlogController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Dentist")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                int doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var (result, error) = await _blogService.CreateDoctorPostAsync(dto, doctorId);

                if (error != null) return BadRequest(new { message = error });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{postId}/update")]
        [Authorize(Roles = "Dentist")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int postId, [FromForm] UpdatePostDto dto)
        {
            try
            {
                int doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var (result, error) = await _blogService.UpdateDoctorPostAsync(postId, dto, doctorId);

                if (error != null)
                {
                    if (error.Contains("غير موجود")) return NotFound(new { message = error });
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
        [Authorize(Roles = "Dentist")]
        public async Task<IActionResult> GetMyPosts()
        {
            try
            {
                int doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var posts = await _blogService.GetDoctorPostsAsync(doctorId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{postId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePost(int postId)
        {
            try
            {
                var (result, error) = await _blogService.ApprovePostAsync(postId);

                if (error != null)
                {
                    if (error.Contains("غير موجود")) return NotFound(new { message = error });
                    return BadRequest(new { message = error });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة قبول المنشور.", error = ex.Message });
            }
        }

        [HttpGet("pending-posts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPosts()
        {
            try
            {
                var pendingPosts = await _blogService.GetPendingDoctorPostsAsync();
                return Ok(pendingPosts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{postId}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPost(int postId)
        {
            try
            {
                var (success, error) = await _blogService.RejectPostAsync(postId);

                if (error != null)
                {
                    if (error.Contains("غير موجود")) return NotFound(new { message = error });
                    return BadRequest(new { message = error });
                }

                // 🎯 تعديل الرسالة لتطابق الواقع الجديد
                return Ok(new { message = "تم رفض المنشور بنجاح وتحويل حالته إلى (مرفوض)، وتم تنبيه الطبيب." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ داخلي أثناء معالجة رفض المنشور.", error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin,Dentist")]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetDoctorNotifications()
        {
            try
            {
                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var notifications = await _blogService.GetNotificationsByRecipientIdAsync(currentUserId);

                var result = notifications.Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    RecipientId = n.RecipientId,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    BlogPostId = n.BlogPostId,
                    BlogPostType = n.BlogPost != null ? n.BlogPost.Type.ToString() : null
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("search")]
        public async Task<IActionResult> SearchBlogPosts([FromForm] string query)
        {
            try
            {
                var (data, error) = await _blogService.SearchBlogPostsServiceAsync(query);

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

}