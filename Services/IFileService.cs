using DentalLab.Api.Models;
using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Services
{
    public interface IFileService
    {
        Task<string> UploadStlToCaseAsync(int caseOrderId, IFormFile file);

        Task<List<FileResource>> GetStlFilesByCaseAsync(int caseOrderId);
    }
}
//