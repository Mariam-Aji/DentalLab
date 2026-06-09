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
    }
}
//