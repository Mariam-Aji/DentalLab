using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Services;

public interface ILabGalleryService
{
    Task<string?> AddLabGalleryAsync(int labId, List<IFormFile> images);
}
