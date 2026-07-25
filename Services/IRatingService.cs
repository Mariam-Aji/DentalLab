using DentalLab.Api.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public interface IRatingService
    {
        Task<object> ProcessQualityRating(int userId, int labId, int qualityScore);
        Task<object> ProcessTimeRating(int userId, int labId, int timeScore);
        Task<object> CalculateAndSaveFinalRatingAsync(int userId, int labId, int timeScore, int qualityScore);

        Task<List<object>> GetTopRatedLabsAsync(int? currentUserId = null);
        Task<List<object>> GetLabsByDoctorLocationAsync(int doctorId);
        Task<List<object>> GetLabsWithScanServiceAsync(int? currentUserId = null);
        Task<object?> GetLabProfileDetailsAsync(int labId, int? currentUserId = null);
        Task<List<object>> GetAvailableLabsAsync(int? currentUserId = null);
        Task<List<LabRatingChartDto>> GetLabsRatingChartDataAsync();
    }
}