using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface IAdvertisementRepository
{
    Task<User> SaveUserAsync(User user);
    Task<bool> IsUserExistsAsync(int userId);
    Task<Advertisement> SaveAdvertisementAsync(Advertisement advertisement);
    Task<List<Advertisement>> GetAllAdvertisementsAsync();
    Task<Advertisement?> GetAdvertisementByIdAsync(int id);
    Task<bool> UpdateAdvertisementAsync(Advertisement advertisement);
    Task<bool> DeleteAdvertisementAsync(Advertisement advertisement);
    Task<List<Advertisement>> GetAdvertisementsForDentistsAsync();
    Task<List<Advertisement>> GetAdvertisementsForLabsAsync();
    Task<User?> GetAdminUserAsync();
    Task<bool> SaveNotificationAsync(Notification notification);
    Task<Advertisement?> GetByIdAsync(int id);
    Task<bool> SaveChangesStatusAsync(Advertisement advertisement);
    Task<List<Advertisement>> GetAdvertisementsByUserIdAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(User user);
    Task<List<User>> SearchLabsByNameAsync(string labName);
    Task<(List<Advertisement> Advertisements, int Count)> GetValidAdvertisementsByUserIdAsync(int userId);
    Task<List<Advertisement>> SearchAdvertisementsAsync(string searchTerm);

}