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
    [Authorize(Roles = "Dentist")] 
    public class DoctorBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public DoctorBlogController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var (result, error) = await _blogService.CreateDoctorPostAsync(dto, doctorId);

                if (error != null)
                    return BadRequest(new { message = error });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
     
        [HttpPut("{postId}/update")]
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
    }
}