using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Services;

public interface IProfilePictureService
{
    /// <summary>Returns the current profile picture URL, or null if not set.</summary>
    Task<string?> GetProfilePictureAsync(int userId);

    /// <summary>Uploads or replaces the profile picture. Returns the new relative URL.</summary>
    Task<(string? url, string? error)> UploadProfilePictureAsync(int userId, IFormFile file);

    /// <summary>Deletes the current profile picture. Returns error message or null on success.</summary>
    Task<string?> DeleteProfilePictureAsync(int userId);
}
