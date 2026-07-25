using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface IRatingRepository
    {
        Task<Rating?> GetExistingRatingAsync(int userId, int labId);
        Task<bool> AddRatingAsync(Rating rating);
        Task<bool> UpdateRatingAsync(Rating rating);
        Task<User?> GetUserByIdAsync(int userId);

        Task<List<object>> GetLabsOrderedByRatingAsync(int? currentUserId = null);
        Task<List<object>> GetLabsByAddressAsync(string address, int? currentUserId = null);
        Task<List<object>> GetLabsByScanVisitServiceAsync(int? currentUserId = null);
        Task<List<object>> GetAvailableLabsAsync(int? currentUserId = null);
        Task<object?> GetLabFullDetailsAsync(int labId, int? currentUserId = null);
        Task<List<LabRatingChartDto>> GetLabsRatingChartDataAsync();
    }
}