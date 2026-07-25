using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class RatingRepository : IRatingRepository
    {
        private readonly ApplicationDbContext _context;
        public RatingRepository(ApplicationDbContext context) => _context = context;

        public async Task<Rating?> GetExistingRatingAsync(int userId, int labId)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.ReviewerId == userId && r.LabId == labId);
        }

        public async Task<bool> AddRatingAsync(Rating rating)
        {
            await _context.Ratings.AddAsync(rating);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRatingAsync(Rating rating)
        {
            _context.Ratings.Update(rating);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        private async Task<List<int>> GetConnectedLabIdsAsync(int? currentUserId)
        {
            if (!currentUserId.HasValue) return new List<int>();

            return await _context.ConnectionRequests
                .AsNoTracking()
                .Where(cr => cr.FromDentistId == currentUserId.Value && cr.Status == ConnectionRequestStatus.Accepted)
                .Select(cr => cr.ToLabId)
                .ToListAsync();
        }

        public async Task<List<object>> GetLabsOrderedByRatingAsync(int? currentUserId = null)
        {
            var connectedLabIds = await GetConnectedLabIdsAsync(currentUserId);

            var result = await _context.Labs
                .AsNoTracking()
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    lab.Description,
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    Country = lab.Owner.CountryPlace,
                    Phone = lab.Owner.Phone,
                    lab.Owner.ProfilePictureUrl,
                    IsConnected = currentUserId.HasValue && connectedLabIds.Contains(lab.Id),
                    AverageRating = lab.Ratings.Any(r => r.Overall > 0)
                                    ? lab.Ratings.Where(r => r.Overall > 0).Average(r => r.Overall)
                                    : lab.AverageRating,
                    RatingsCount = lab.Ratings.Count
                })
                .OrderByDescending(l => l.AverageRating)
                .ToListAsync();

            return result.Cast<object>().ToList();
        }

        public async Task<List<object>> GetLabsByAddressAsync(string address, int? currentUserId = null)
        {
            var connectedLabIds = await GetConnectedLabIdsAsync(currentUserId);

            var result = await _context.Labs
                .AsNoTracking()
                .Where(l => l.Owner.AddressPlace == address)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    Country = lab.Owner.CountryPlace,
                    Phone = lab.Owner.Phone,
                    lab.Owner.ProfilePictureUrl,
                    IsConnected = currentUserId.HasValue && connectedLabIds.Contains(lab.Id),
                    AverageRating = lab.Ratings.Any(r => r.Overall > 0)
                                    ? lab.Ratings.Where(r => r.Overall > 0).Average(r => r.Overall)
                                    : lab.AverageRating,
                    RatingsCount = lab.Ratings.Count,
                    Availability = lab.Availability.ToString()
                })
                .ToListAsync();

            return result.Cast<object>().ToList();
        }

        public async Task<List<object>> GetLabsByScanVisitServiceAsync(int? currentUserId = null)
        {
            var connectedLabIds = await GetConnectedLabIdsAsync(currentUserId);

            var result = await _context.Labs
                .AsNoTracking()
                .Where(l => l.HasScanVisitService == true)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    Country = lab.Owner.CountryPlace,
                    Phone = lab.Owner.Phone,
                    lab.Owner.ProfilePictureUrl,
                    IsConnected = currentUserId.HasValue && connectedLabIds.Contains(lab.Id),
                    AverageRating = lab.Ratings.Any(r => r.Overall > 0)
                                    ? lab.Ratings.Where(r => r.Overall > 0).Average(r => r.Overall)
                                    : lab.AverageRating,
                    RatingsCount = lab.Ratings.Count,
                    lab.HasScanVisitService
                })
                .OrderByDescending(l => l.AverageRating)
                .ToListAsync();

            return result.Cast<object>().ToList();
        }

        public async Task<List<object>> GetAvailableLabsAsync(int? currentUserId = null)
        {
            var connectedLabIds = await GetConnectedLabIdsAsync(currentUserId);

            var result = await _context.Labs
                .AsNoTracking()
                .Where(l => l.Availability == AvailabilityStatus.Available)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    lab.Description,
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    Country = lab.Owner.CountryPlace,
                    Phone = lab.Owner.Phone,
                    lab.Owner.ProfilePictureUrl,
                    IsConnected = currentUserId.HasValue && connectedLabIds.Contains(lab.Id),
                    AverageRating = lab.Ratings.Any(r => r.Overall > 0)
                                    ? lab.Ratings.Where(r => r.Overall > 0).Average(r => r.Overall)
                                    : lab.AverageRating,
                    RatingsCount = lab.Ratings.Count,
                    lab.HasScanVisitService,
                    Availability = lab.Availability.ToString()
                })
                .OrderByDescending(l => l.AverageRating)
                .ToListAsync();

            return result.Cast<object>().ToList();
        }

        public async Task<object?> GetLabFullDetailsAsync(int labId, int? currentUserId = null)
        {
            string connectionStatus = "NotConnected";
            bool isConnected = false;

            if (currentUserId.HasValue)
            {
                var connectionRequest = await _context.ConnectionRequests
                    .AsNoTracking()
                    .Where(cr => cr.FromDentistId == currentUserId.Value && cr.ToLabId == labId)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .FirstOrDefaultAsync();

                if (connectionRequest != null)
                {
                    connectionStatus = connectionRequest.Status.ToString();
                    isConnected = connectionRequest.Status == ConnectionRequestStatus.Accepted;
                }
            }

            return await _context.Labs
                .AsNoTracking()
                .Where(l => l.Id == labId)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    lab.Description,
                    lab.YearsOfExperience,
                    Availability = lab.Availability.ToString(),
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    Country = lab.Owner.CountryPlace,
                    Phone = lab.Owner.Phone,
                    lab.Owner.ProfilePictureUrl,
                    lab.HasScanVisitService,

                    ConnectionStatus = connectionStatus,
                    IsConnected = isConnected,

                    lab.Materials,
                    lab.Specialties,

                    RatingSummary = new
                    {
                        AverageOverall = lab.Ratings.Any() ? lab.Ratings.Average(r => r.Overall) : lab.AverageRating,
                        AverageQuality = lab.Ratings.Any() ? lab.Ratings.Average(r => r.Quality) : 0,
                        AverageTimeCommitment = lab.Ratings.Any() ? lab.Ratings.Average(r => r.TimeCommitment) : 0,
                        TotalReviews = lab.Ratings.Count
                    },
                    Reviews = lab.Ratings.Select(r => new
                    {
                        r.Id,
                        ReviewerName = r.Reviewer != null ? r.Reviewer.Name : "Unknown",
                        ReviewerPicture = r.Reviewer != null ? r.Reviewer.ProfilePictureUrl : null,
                        r.Overall,
                        r.Quality,
                        r.TimeCommitment,
                        r.Comment,
                        r.CreatedAt
                    }).OrderByDescending(r => r.CreatedAt).ToList(),

                    Prices = lab.Prices.Select(p => new
                    {
                        Type = p.CompensationType.ToString(),
                        Price = p.UnitPrice,
                        p.Notes
                    }).ToList(),

                    GalleryImages = lab.Gallery.Select(img => new
                    {
                        Url = img.Path,
                        Name = img.Path
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }


        public async Task<List<LabRatingChartDto>> GetLabsRatingChartDataAsync()
        {
            return await _context.Labs
                .AsNoTracking()
                .Select(lab => new LabRatingChartDto
                {
                    LabId = lab.Id,
                    LabName = lab.Owner.NamePlace ?? lab.Owner.Name,
                    AverageOverallRating = lab.Ratings.Any(r => r.Overall > 0)
                                           ? lab.Ratings.Where(r => r.Overall > 0).Average(r => (double)r.Overall)
                                           : lab.AverageRating,
                    AverageQualityRating = lab.Ratings.Any(r => r.Quality > 0)
                                           ? lab.Ratings.Where(r => r.Quality > 0).Average(r => (double)r.Quality)
                                           : 0,
                    AverageTimeCommitmentRating = lab.Ratings.Any(r => r.TimeCommitment > 0)
                                                  ? lab.Ratings.Where(r => r.TimeCommitment > 0).Average(r => (double)r.TimeCommitment)
                                                  : 0,
                    TotalReviews = lab.Ratings.Count
                })
                .OrderByDescending(l => l.AverageOverallRating) // ترتبيها تنازلياً حسب الأعلى تقييماً للمخطط البياني
                .ToListAsync();
        }
    }
}