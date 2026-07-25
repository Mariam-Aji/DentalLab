using DentalLab.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class ProfilePictureService : IProfilePictureService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProfilePictureService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<string?> GetProfilePictureAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.ProfilePictureUrl;
    }

    public async Task<(string? url, string? error)> UploadProfilePictureAsync(int userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (null, "No file provided.");

        if (file.Length > MaxFileSizeBytes)
            return (null, "File size exceeds 5 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(ext))
            return (null, $"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (null, "User not found.");

        // Delete old picture file from disk if it exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            DeleteFileFromDisk(user.ProfilePictureUrl);

        // Save new file
        var uploadDir = Path.Combine(_env.ContentRootPath, "uploads", "profile-pictures");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var relativeUrl = $"uploads/profile-pictures/{fileName}";

        user.ProfilePictureUrl = relativeUrl;
        await _context.SaveChangesAsync();

        return (relativeUrl, null);
    }

    public async Task<string?> DeleteProfilePictureAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return "User not found.";

        if (string.IsNullOrEmpty(user.ProfilePictureUrl))
            return "No profile picture to delete.";

        DeleteFileFromDisk(user.ProfilePictureUrl);

        user.ProfilePictureUrl = null;
        await _context.SaveChangesAsync();

        return null; // success
    }

    // -------------------------------------------------------
    private void DeleteFileFromDisk(string relativePath)
    {
        try
        {
            var fullPath = Path.Combine(_env.ContentRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Non-critical – ignore disk errors during cleanup
        }
    }
}
