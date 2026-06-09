using DentalLab.Api.Dtos;
using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface ILabProfileService
{
    Task<Lab?> GetProfileAsync(int userId);
    Task<(Lab? lab, string? error)> UpdateProfileAsync(int userId, LabProfileUpdateDto dto);
    Task<(LabPrice? price, string? error)> AddPriceAsync(int userId, LabPriceUpsertDto dto);
    Task<string?> UpdatePriceAsync(int userId, int priceId, LabPriceUpsertDto dto);
    Task<string?> DeletePriceAsync(int userId, int priceId);
    Task<string?> DeleteGalleryAsync(int userId, int fileId);
}
