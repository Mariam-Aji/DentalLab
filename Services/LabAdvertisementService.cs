using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabAdvertisementService : ILabAdvertisementService
{
    private readonly ApplicationDbContext _context;

    public LabAdvertisementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LabAdvertisementDto>> GetAdsForLabAsync()
    {
        var now = DateTime.UtcNow;

        var ads = await _context.Advertisements
            .AsNoTracking()
            .Where(a =>
                a.IsPaid == true &&
                a.IsActive == true &&
                (a.ExpiresAt == null || a.ExpiresAt > now) &&
                (a.Target == TargetAudience.Labs || a.Target == TargetAudience.Both))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return ads.Select(a => new LabAdvertisementDto
        {
            Id        = a.Id,
            Title     = a.Title,
            Content   = a.Content,
            Target    = a.Target.ToString(),
            CreatedAt = a.CreatedAt,
            ExpiresAt = a.ExpiresAt,
            Images    = string.IsNullOrEmpty(a.ImageUrl)
                ? new List<string>()
                : a.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
        }).ToList();
    }
}
