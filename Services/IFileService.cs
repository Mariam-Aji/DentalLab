using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Services
{
    public interface IFileService
    {
        Task<string> UploadStlToCaseAsync(int caseOrderId, IFormFile file);
    }
}
//