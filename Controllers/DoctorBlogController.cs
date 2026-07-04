using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly INotificationService _notificationService;
        private readonly DentalLab.Api.Data.ApplicationDbContext _db;

        public DoctorBlogController(
            IBlogService blogService,
            INotificationService notificationService,
            DentalLab.Api.Data.ApplicationDbContext db)
        {
            _blogService         = blogService;
            _notificationService = notificationService;
            _db                  = db;
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
                // استخدام الطريقة المباشرة والقراءة الآمنة من التوكن كما في كودكِ
                int doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var posts = await _blogService.GetDoctorPostsAsync(doctorId);

                // 🎯 التحقق مما إذا كان الطبيب ليس لديه أي منشورات
                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لم تقم بنشر أي منشورات بعد." });
                }

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
        [HttpGet("pending-lab-posts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingLabPosts()
        {
            try
            {
                var pendingLabPosts = await _blogService.GetPendingLabPostsAsync();
                return Ok(pendingLabPosts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("all-pending-posts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPendingPosts()
        {
            try
            {
                var pendingPosts = await _blogService.GetPendingAllPostsAsync();
                return Ok(pendingPosts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // 1️⃣ تابع عرض منشورات الأطباء المقبولة (الترتيب: الأحدث أولاً)
        [HttpGet("approved-doctor-posts")]
        [Authorize]
        public async Task<IActionResult> GetApprovedDoctorPosts()
        {
            try
            {
                var posts = await _blogService.GetApprovedDoctorPostsAsync();

                // التحقق من وجود منشورات
                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد منشورات مقبولة للأطباء حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2️⃣ تابع عرض منشورات المخابر المقبولة (الترتيب: الأحدث أولاً)
        [HttpGet("approved-lab-posts")]
        [Authorize]
        public async Task<IActionResult> GetApprovedLabPosts()
        {
            try
            {
                var posts = await _blogService.GetApprovedLabPostsAsync();

                // التحقق من وجود منشورات
                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد منشورات مقبولة للمخابر حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3️⃣ تابع عرض منشورات الأطباء والمخابر معاً (الترتيب: الأحدث أولاً للكل)
        [HttpGet("approved-all-posts")]
        [Authorize(Roles = "Admin,Dentist")]

        public async Task<IActionResult> GetApprovedAllPosts()
        {
            try
            {
                var posts = await _blogService.GetApprovedAllPostsAsync();

                // التحقق من وجود منشورات
                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد أي منشورات مقبولة حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // 1. عرض المنشورات المرفوضة للطبيب
        [HttpGet("rejected-doctor-posts")]
        [Authorize(Roles = "Admin")] // يفضل حصرها بالأدمن
        public async Task<IActionResult> GetRejectedDoctorPosts()
        {
            try
            {
                var posts = await _blogService.GetRejectedDoctorPostsAsync();

                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد منشورات مرفوضة للأطباء حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. عرض المنشورات المرفوضة للمخبري
        [HttpGet("rejected-lab-posts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRejectedLabPosts()
        {
            try
            {
                var posts = await _blogService.GetRejectedLabPostsAsync();

                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد منشورات مرفوضة للمخابر حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. عرض المنشورات المرفوضة للنوعين معاً
        [HttpGet("rejected-all-posts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRejectedAllPosts()
        {
            try
            {
                var posts = await _blogService.GetRejectedAllPostsAsync();

                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "لا توجد أي منشورات مرفوضة حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("my-pending-posts")]
        [Authorize(Roles = "Dentist")] // تأكدي من تطابق اسم الـ Role المعتمدة لديكِ
        public async Task<IActionResult> GetMyPendingPosts()
        {
            try
            {
                // استخراج المعرف من التوكن
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int doctorId))
                {
                    return Unauthorized(new { message = "فشل التحقق من الهوية، التوكن غير صالح أو منتهي الصلاحية." });
                }

                var posts = await _blogService.GetPendingPostsByDoctorIdAsync(doctorId);

                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "ليست لديك أي منشورات معلقة حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-rejected-posts")]
        [Authorize(Roles = "Dentist")]
        public async Task<IActionResult> GetMyRejectedPosts()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int doctorId))
                {
                    return Unauthorized(new { message = "فشل التحقق من الهوية، التوكن غير صالح أو منتهي الصلاحية." });
                }

                var posts = await _blogService.GetRejectedPostsByDoctorIdAsync(doctorId);

                if (posts == null || !posts.Any())
                {
                    return Ok(new { message = "ليست لديك أي منشورات مرفوضة حالياً." });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("delete-doctor-post/{id}")]
        [Authorize(Roles = "Dentist")]

        public async Task<IActionResult> DeleteDoctorPost(int id)
        {
            try
            {
                var result = await _blogService.DeleteDoctorPostAsync(id);

                if (!result)
                {
                    return NotFound(new { message = "عذراً، المنشور غير موجود أو لا تملك صلاحية حذفه." });
                }

                return Ok(new { message = "تم حذف المنشور بنجاح." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

}