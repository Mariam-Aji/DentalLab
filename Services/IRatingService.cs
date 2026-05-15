public interface IRatingService
{
    Task<object> ProcessQualityRating(int userId, int labId, int qualityScore);
    Task<object> ProcessTimeRating(int userId, int labId, int timeScore);
    Task<object> CalculateAndSaveFinalRatingAsync(int userId, int labId, int timeScore, int qualityScore);
    Task<List<object>> GetTopRatedLabsAsync();
    Task<List<object>> GetLabsByDoctorLocationAsync(int doctorId);
    Task<object?> GetLabProfileDetailsAsync(int labId);
    Task<List<object>> GetLabsWithScanServiceAsync();
}