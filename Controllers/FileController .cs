using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload-stl/{caseOrderId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadStl(int caseOrderId, IFormFile file)
        {
            try
            {
                var result = await _fileService.UploadStlToCaseAsync(caseOrderId, file);
                return Ok(new { path = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // إرجاع HTTP 400 في حال عدم وجود ملف
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                // إرجاع HTTP 404 في حال عدم وجود الطلب
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ غير متوقع: {ex.Message}" });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("stl/{caseOrderId}")]
        public async Task<IActionResult> GetStlFilesByCase(int caseOrderId)
        {
            try
            {
                var files = await _fileService.GetStlFilesByCaseAsync(caseOrderId);

                if (files == null || !files.Any())
                {
                    return NotFound(new { message = "لا توجد ملفات STL مرفوعة لهذه الطلبية." });
                }

                var response = files.Select(f => new
                {
                    f.Id,
                    f.Path,
                    fullUrl = $"{Request.Scheme}://{Request.Host}/{f.Path}", // لتوليد الرابط الكامل للملف مباشرة
                    f.UploadedAt,
                    f.Type
                });

                return Ok(new
                {
                    caseOrderId = caseOrderId,
                    count = files.Count,
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
//