using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        public RatingService(IRatingRepository ratingRepository) => _ratingRepository = ratingRepository;

        public async Task<object> ProcessQualityRating(int userId, int labId, int qualityScore)
        {
            return await ProcessPartialRatingAsync(userId, labId, qualityScore, isQuality: true);
        }

        public async Task<object> ProcessTimeRating(int userId, int labId, int timeScore)
        {
            return await ProcessPartialRatingAsync(userId, labId, timeScore, isQuality: false);
        }

        private async Task<object> ProcessPartialRatingAsync(int userId, int labId, int score, bool isQuality)
        {
            var existingRating = await _ratingRepository.GetExistingRatingAsync(userId, labId);

            if (existingRating == null)
            {
                var lab = await _ratingRepository.GetLabFullDetailsAsync(labId);
                if (lab == null)
                {
                    return new
                    {
                        Success = false,
                        Message = $"المخبر ذو الرقم {labId} غير موجود."
                    };
                }
            }

            var rating = existingRating ?? new Rating
            {
                LabId = labId,
                ReviewerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            int clampedScore = Clamp(score);
            if (isQuality)
            {
                rating.Quality = clampedScore;
            }
            else
            {
                rating.TimeCommitment = clampedScore;
            }

            rating.Overall = CalculateOverall(rating.Quality, rating.TimeCommitment);

            await SaveOrUpdate(rating);

            return new
            {
                Success = true,
                RatingId = rating.Id,
                Value = isQuality ? rating.Quality : rating.TimeCommitment,
                LabId = rating.LabId
            };
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

        public async Task<List<object>> GetTopRatedLabsAsync(int? currentUserId = null)
        {
            return await _ratingRepository.GetLabsOrderedByRatingAsync(currentUserId);
        }

        public async Task<List<object>> GetLabsByDoctorLocationAsync(int doctorId)
        {
            var doctor = await _ratingRepository.GetUserByIdAsync(doctorId);

            if (doctor == null || string.IsNullOrEmpty(doctor.AddressPlace))
            {
                return new List<object>();
            }

            return await _ratingRepository.GetLabsByAddressAsync(doctor.AddressPlace, doctorId);
        }

        public async Task<List<object>> GetLabsWithScanServiceAsync(int? currentUserId = null)
        {
            return await _ratingRepository.GetLabsByScanVisitServiceAsync(currentUserId);
        }

        public async Task<object?> GetLabProfileDetailsAsync(int labId, int? currentUserId = null)
        {
            return await _ratingRepository.GetLabFullDetailsAsync(labId, currentUserId);
        }

        public async Task<List<object>> GetAvailableLabsAsync(int? currentUserId = null)
        {
            return await _ratingRepository.GetAvailableLabsAsync(currentUserId);
        }
        public async Task<List<LabRatingChartDto>> GetLabsRatingChartDataAsync()
        {
            return await _ratingRepository.GetLabsRatingChartDataAsync();
        }
    }
}