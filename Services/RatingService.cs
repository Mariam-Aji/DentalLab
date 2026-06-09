using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;
    public RatingService(IRatingRepository ratingRepository) => _ratingRepository = ratingRepository;

    public async Task<object> ProcessQualityRating(int userId, int labId, int qualityScore)
    {
        var rating = await GetOrCreateRating(userId, labId);
        rating.Quality = Clamp(qualityScore);

        rating.Overall = CalculateOverall(rating.Quality, rating.TimeCommitment);

        await SaveOrUpdate(rating);

        return new { RatingId = rating.Id, Value = rating.Quality, LabId = rating.LabId };
    }

    public async Task<object> ProcessTimeRating(int userId, int labId, int timeScore)
    {
        var rating = await GetOrCreateRating(userId, labId);
        rating.TimeCommitment = Clamp(timeScore);

        rating.Overall = CalculateOverall(rating.Quality, rating.TimeCommitment);

        await SaveOrUpdate(rating);

        return new { RatingId = rating.Id, Value = rating.TimeCommitment, LabId = rating.LabId };
    }

    public async Task<object> CalculateAndSaveFinalRatingAsync(int userId, int labId, int timeScore, int qualityScore)
    {
        var rating = await GetOrCreateRating(userId, labId);
        if (qualityScore > 0) rating.Quality = Clamp(qualityScore);
        if (timeScore > 0) rating.TimeCommitment = Clamp(timeScore);

        rating.Overall = CalculateOverall(rating.Quality, rating.TimeCommitment);
        await SaveOrUpdate(rating);

        return new { Success = true, RatingId = rating.Id, Overall = rating.Overall };
    }


    private async Task<Rating> GetOrCreateRating(int userId, int labId)
    {
        var existing = await _ratingRepository.GetExistingRatingAsync(userId, labId);

        return existing ?? new Rating { LabId = labId, ReviewerId = userId, CreatedAt = DateTime.UtcNow };
    }

    private int CalculateOverall(int q, int t)
    {
        if (q == 0 || t == 0) return 0;
        return (int)Math.Round((q + t) / 2.0);
    }

    private int Clamp(int score) => (score < 1) ? 1 : (score > 5) ? 5 : score;

    private async Task SaveOrUpdate(Rating r)
    {
        if (r.Id == 0) await _ratingRepository.AddRatingAsync(r);
        else await _ratingRepository.UpdateRatingAsync(r);
    }
    public async Task<List<object>> GetTopRatedLabsAsync()
    {
        return await _ratingRepository.GetLabsOrderedByRatingAsync();
    }
    public async Task<List<object>> GetLabsByDoctorLocationAsync(int doctorId)
    {
        var doctor = await _ratingRepository.GetUserByIdAsync(doctorId);

        if (doctor == null || string.IsNullOrEmpty(doctor.AddressPlace))
        {
            return new List<object>();
        }

        return await _ratingRepository.GetLabsByAddressAsync(doctor.AddressPlace);
    }
    public async Task<object?> GetLabProfileDetailsAsync(int labId)
    {
        var labDetails = await _ratingRepository.GetLabFullDetailsAsync(labId);

        if (labDetails == null)
            return null; 

        return labDetails;
    }
    public async Task<List<object>> GetLabsWithScanServiceAsync()
    {
        return await _ratingRepository.GetLabsByScanVisitServiceAsync();
    }
    //
}