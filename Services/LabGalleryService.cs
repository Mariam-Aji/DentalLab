using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace DentalLab.Api.Services;

public class LabGalleryService : ILabGalleryService
{
    private readonly IAccountsRepository _repo;
    private readonly IWebHostEnvironment _env;

    public LabGalleryService(IAccountsRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    public async Task<string?> AddLabGalleryAsync(int labId, List<IFormFile> images)
    {
        if (images == null || images.Count == 0) return "Images are required.";

        var lab = await _repo.GetLabByIdTrackingAsync(labId);
        if (lab == null) return "Lab not found.";

        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "labs", labId.ToString(), "gallery");
        Directory.CreateDirectory(uploadsRoot);

        foreach (var image in images)
        {
            var error = ValidateGalleryImage(image);
            if (error != null) return error;

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads", "labs", labId.ToString(), "gallery", fileName)
                .Replace("\\", "/");

            await _repo.AddFileResourceAsync(new FileResource
            {
                LabId = labId,
                Path = relativePath,
                Type = FileType.LabGallery,
                UploadedAt = DateTime.UtcNow
            });
        }

        await _repo.SaveChangesAsync();
        return null;
    }

    private string? ValidateGalleryImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return "Image is required.";

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes) return "Image must be 5MB or less.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowed.Contains(ext)) return "Image must be JPG or PNG.";

        return null;
    }
}
