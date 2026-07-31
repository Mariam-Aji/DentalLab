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
            var result = await _fileService.UploadStlToCaseAsync(caseOrderId, file);
            return Ok(new { path = result });
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