using DentalLab.Api.Data;
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
        public async Task<List<object>> GetLabsOrderedByRatingAsync()
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    Description = lab.Description,
                    City = lab.Owner.CityPlace,
                    AverageRating = lab.Ratings.Any(r => r.Overall > 0)
                                    ? lab.Ratings.Where(r => r.Overall > 0).Average(r => r.Overall)
                                    : 0
                })
                .OrderByDescending(l => l.AverageRating)
                .Cast<object>()
                .ToListAsync();
        }
        public async Task<List<object>> GetLabsByAddressAsync(string address)
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Where(l => l.Owner.AddressPlace == address)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    Address = lab.Owner.AddressPlace,
                    City = lab.Owner.CityPlace,
                    AverageRating = lab.AverageRating,
                    Availability = lab.Availability.ToString()
                })
                .Cast<object>()
                .ToListAsync();
        }
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }
        public async Task<object?> GetLabFullDetailsAsync(int labId)
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Include(l => l.Prices)
                .Include(l => l.Gallery)
                .Where(l => l.Id == labId)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    Description = lab.Description,
                    YearsOfExperience = lab.YearsOfExperience,


                    Availability = lab.Availability.ToString(),



                    Materials = lab.Materials,
                    Specialties = lab.Specialties,
                    AverageRating = lab.AverageRating,

                    Prices = lab.Prices.Select(p => new
                    {
                        Type = p.CompensationType.ToString(),
                        Price = p.UnitPrice,
                        Notes = p.Notes
                    }).ToList(),

                    GalleryImages = lab.Gallery.Select(img => new
                    {
                        Url = img.Path,
                        Name = img.Path
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<object>> GetLabsByScanVisitServiceAsync()
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Where(l => l.HasScanVisitService == true)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    AverageRating = lab.AverageRating,
                    HasScanVisitService = lab.HasScanVisitService,
                    City = lab.Owner.CityPlace
                })
                .OrderByDescending(l => l.AverageRating)
                .Cast<object>()
                .ToListAsync();
        }


        public async Task<object?> GetLabFullDetailsAsync(int labId, int? currentUserId = null)
        {
            string connectionStatus = "NotConnected";

            if (currentUserId.HasValue)
            {
                var connectionRequest = await _context.ConnectionRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cr => cr.FromDentistId == currentUserId.Value && cr.ToLabId == labId);

                if (connectionRequest != null)
                {
                    connectionStatus = connectionRequest.Status.ToString(); 
                }
            }

            return await _context.Labs
                .Include(l => l.Owner)
                .Include(l => l.Prices)
                .Include(l => l.Gallery)
                .Where(l => l.Id == labId)
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    Description = lab.Description,
                    YearsOfExperience = lab.YearsOfExperience,
                    Availability = lab.Availability.ToString(),

                    ProfilePictureUrl = lab.Owner.ProfilePictureUrl,

                    HasScanVisitService = lab.HasScanVisitService,
                    ConnectionStatus = connectionStatus,

                    Materials = lab.Materials,
                    Specialties = lab.Specialties,
                    AverageRating = lab.AverageRating,

                    Prices = lab.Prices.Select(p => new
                    {
                        Type = p.CompensationType.ToString(),
                        Price = p.UnitPrice,
                        Notes = p.Notes
                    }).ToList(),

                    GalleryImages = lab.Gallery.Select(img => new
                    {
                        Url = img.Path,
                        Name = img.Path
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<object>> GetAvailableLabsAsync()
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Where(l => l.Availability == AvailabilityStatus.Available) 
                .Select(lab => new
                {
                    lab.Id,
                    LabName = lab.Owner.Name,
                    Description = lab.Description,
                    City = lab.Owner.CityPlace,
                    Address = lab.Owner.AddressPlace,

                    ProfilePictureUrl = lab.Owner.ProfilePictureUrl,

                    AverageRating = lab.AverageRating,
                    HasScanVisitService = lab.HasScanVisitService,
                    Availability = lab.Availability.ToString()
                })
                .OrderByDescending(l => l.AverageRating) 
                .Cast<object>()
                .ToListAsync();
        }

    } }