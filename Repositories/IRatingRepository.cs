using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface IRatingRepository
    {
        Task<Rating?> GetExistingRatingAsync(int userId, int labId);

        Task<bool> AddRatingAsync(Rating rating);
        Task<bool> UpdateRatingAsync(Rating rating);
        Task<List<object>> GetLabsOrderedByRatingAsync();
        Task<List<object>> GetLabsByAddressAsync(string address);
        Task<User?> GetUserByIdAsync(int userId);
        Task<object?> GetLabFullDetailsAsync(int labId);
        Task<List<object>> GetLabsByScanVisitServiceAsync();
        //
        Task<object?> GetLabFullDetailsAsync(int labId, int? currentUserId = null);
        Task<List<object>> GetAvailableLabsAsync();

    }

}