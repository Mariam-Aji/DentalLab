using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DentalLab.Api.Services;

public class LabProfileService : ILabProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public LabProfileService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public Task<Lab?> GetProfileAsync(int userId)
    {
        return _db.Labs.AsNoTracking()
            .Include(l => l.Owner)
            .Include(l => l.Gallery)
            .Include(l => l.Prices)
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }

    public async Task<(Lab? lab, string? error)> UpdateProfileAsync(int userId, LabProfileUpdateDto dto)
    {
        var lab = await _db.Labs
            .Include(l => l.Owner)
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (lab == null) return (null, "Lab not found.");

        var user = lab.Owner;
        if (user == null) return (null, "Lab owner not found.");

        if (dto.Name != null)
        {
            var trimmed = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return (null, "Name cannot be empty.");
            user.Name = trimmed;
        }

        if (dto.Phone != null) user.Phone = NormalizeOptionalString(dto.Phone);
        if (dto.NamePlace != null) user.NamePlace = NormalizeOptionalString(dto.NamePlace);
        if (dto.AddressPlace != null) user.AddressPlace = NormalizeOptionalString(dto.AddressPlace);
        if (dto.CityPlace != null) user.CityPlace = NormalizeOptionalString(dto.CityPlace);
        if (dto.CountryPlace != null) user.CountryPlace = NormalizeOptionalString(dto.CountryPlace);

        if (dto.Description != null) lab.Description = NormalizeOptionalString(dto.Description);
        if (dto.YearsOfExperience.HasValue) lab.YearsOfExperience = dto.YearsOfExperience.Value;
        if (dto.Specialties != null) lab.Specialties = NormalizeStringList(dto.Specialties);
        if (dto.Materials != null) lab.Materials = NormalizeStringList(dto.Materials);
        if (dto.Availability.HasValue) lab.Availability = dto.Availability.Value;
        if (dto.HasScanVisitService.HasValue) lab.HasScanVisitService = dto.HasScanVisitService.Value;

        await _db.SaveChangesAsync();

        var refreshed = await GetProfileAsync(userId);
        return (refreshed, null);
    }

    public async Task<(LabPrice? price, string? error)> AddPriceAsync(int userId, LabPriceUpsertDto dto)
    {
        var labId = await _db.Labs
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (labId == null) return (null, "Lab not found.");

        var price = new LabPrice
        {
            LabId = labId.Value,
            CompensationType = dto.CompensationType,
            UnitPrice = dto.UnitPrice,
            Notes = NormalizeOptionalString(dto.Notes),
            UpdatedAt = DateTime.UtcNow
        };

        _db.LabPrices.Add(price);
        await _db.SaveChangesAsync();

        return (price, null);
    }

    public async Task<string?> UpdatePriceAsync(int userId, int priceId, LabPriceUpsertDto dto)
    {
        var labId = await _db.Labs
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (labId == null) return "Lab not found.";

        var price = await _db.LabPrices
            .FirstOrDefaultAsync(p => p.Id == priceId && p.LabId == labId.Value);

        if (price == null) return "Price not found.";

        price.CompensationType = dto.CompensationType;
        price.UnitPrice = dto.UnitPrice;
        price.Notes = NormalizeOptionalString(dto.Notes);
        price.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> DeletePriceAsync(int userId, int priceId)
    {
        var labId = await _db.Labs
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (labId == null) return "Lab not found.";

        var price = await _db.LabPrices
            .FirstOrDefaultAsync(p => p.Id == priceId && p.LabId == labId.Value);

        if (price == null) return "Price not found.";

        _db.LabPrices.Remove(price);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> DeleteGalleryAsync(int userId, int fileId)
    {
        var labId = await _db.Labs
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (labId == null) return "Lab not found.";

        var file = await _db.FileResources
            .FirstOrDefaultAsync(f => f.Id == fileId && f.LabId == labId.Value && f.Type == FileType.LabGallery);

        if (file == null) return "Image not found.";

        var relativePath = file.Path.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_env.ContentRootPath, relativePath);

        _db.FileResources.Remove(file);
        await _db.SaveChangesAsync();

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return null;
    }

    private static string? NormalizeOptionalString(string? value)
    {
        if (value == null) return null;
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static List<string> NormalizeStringList(List<string> values)
    {
        return values
            .Select(v => v?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }
}
