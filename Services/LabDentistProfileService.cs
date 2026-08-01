using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabDentistProfileService : ILabDentistProfileService
{
    private readonly ApplicationDbContext _db;

    public LabDentistProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(DentistProfileDto? result, string? error)> GetDentistProfileAsync(int dentistId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == dentistId && u.Role == UserRole.Dentist)
            .Select(u => new DentistProfileDto
            {
                Id                = u.Id,
                Name              = u.Name,
                Email             = u.Email,
                Phone             = u.Phone,
                ClinicName        = u.NamePlace,
                ClinicAddress     = u.AddressPlace,
                City              = u.CityPlace,
                Country           = u.CountryPlace,
                ProfilePictureUrl = u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return (null, "Dentist not found.");

        return (user, null);
    }
}
