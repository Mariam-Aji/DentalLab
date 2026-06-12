using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public interface IAdvertisementService
{
    Task<(User? result, string? error)> CreateADSClientByAdminAsync(CreateADSClientDto dto);
    Task<(Advertisement? result, string? error)> CreateAdvertisementAsync(int userId, CreateAdvertisementDto dto);
    Task<List<Advertisement>> GetAllAdvertisementsForAdminAsync();

    Task<(Advertisement? result, string? error)> UpdateAdvertisementAsync(int advId, UpdateAdvertisementDto dto);

    Task<(bool success, string? error)> DeleteAdvertisementAsync(int advId);
    Task<(Advertisement? result, string? error)> ToggleAdStatusAsync(int advId);
    Task<List<Advertisement>> GetAdvertisementsForDentistsAsync();
    Task<List<Advertisement>> GetAdvertisementsForLabsAsync();
    Task<(Advertisement? result, string? error)> CreateAdvertisementByDoctorAsync(int doctorId, CreateAdvertisementDto dto);
    Task<(bool isActivated, string? errorMessage)> ActivateDoctorAdvertisementAsync(int advertisementId, int userId, decimal price);
    Task<List<Advertisement>> GetAdvertisementsByUserIdAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task<(User? result, string? error)> UpdateUserAsync(int userId, UpdateUserDto dto);
    Task<(bool success, string? error)> DeleteUserAsync(int userId);
    Task<List<object>> SearchLabsAsync(string name);
    Task<(object? Data, string? Error)> GetUserActiveAdvertisementsWithCountAsync(int userId);
    Task<(object? Data, string? Error)> SearchAdvertisementsServiceAsync(string searchTerm);

}