using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabConnectedDoctorsService : ILabConnectedDoctorsService
{
    private readonly ILabConnectedDoctorsRepository _repo;
    private readonly ILabProfileService _labProfile;

    public LabConnectedDoctorsService(
        ILabConnectedDoctorsRepository repo,
        ILabProfileService labProfile)
    {
        _repo       = repo;
        _labProfile = labProfile;
    }

    public async Task<(List<ConnectedDoctorDto>? doctors, string? error)> GetConnectedDoctorsAsync(
        int labUserId)
    {
        var lab = await _labProfile.GetProfileAsync(labUserId);
        if (lab == null) return (null, "المخبر غير موجود.");

        var users   = await _repo.GetConnectedDoctorsAsync(lab.Id);
        var doctors = users.Select(MapDoctor).ToList();
        return (doctors, null);
    }

    public async Task<(List<LabPendingOrderDto>? orders, string? error)> GetOrdersByDentistAsync(
        int labUserId, int dentistId)
    {
        var lab = await _labProfile.GetProfileAsync(labUserId);
        if (lab == null) return (null, "المخبر غير موجود.");

        var orders = await _repo.GetOrdersByDentistForLabAsync(lab.Id, dentistId);
        var result = orders.Select(MapOrder).ToList();
        return (result, null);
    }

    public async Task<string?> DisconnectDoctorAsync(int labUserId, int dentistId)
    {
        var lab = await _labProfile.GetProfileAsync(labUserId);
        if (lab == null) return "المخبر غير موجود.";

        var connection = await _repo.GetAcceptedConnectionAsync(lab.Id, dentistId);
        if (connection == null) return "لا يوجد اتصال نشط مع هذا الطبيب.";

        await _repo.DeleteConnectionAsync(connection);
        return null;
    }

    // ---- helpers ----

    private static ConnectedDoctorDto MapDoctor(User u) => new()
    {
        Id             = u.Id,
        Name           = u.Name,
        Email          = u.Email,
        Phone          = u.Phone,
        ClinicName     = u.NamePlace,
        ClinicAddress  = u.AddressPlace,
        City           = u.CityPlace,
        Country        = u.CountryPlace
    };

    private static LabPendingOrderDto MapOrder(CaseOrder co) => new()
    {
        OrderId        = co.Id,
        Title          = co.Title,
        Status         = co.Status.ToString(),
        ImpressionStage = co.ImpressionStage.ToString(),
        ImpressionType  = co.ImpressionType.ToString(),
        Shade          = co.Shade,
        IsTemporary    = co.IsTemporary,
        IsUrgent       = co.IsUrgent,
        DeliveryDate   = co.DeliveryDate,
        Notes          = co.Notes,
        EstimatedPrice = co.EstimatedPrice,
        FinalPrice     = co.FinalPrice,
        CreatedAt      = co.CreatedAt,
        HasAccessories = co.HasAccessories,

        DentistId            = co.CreatedById,
        DentistName          = co.CreatedBy?.Name ?? "",
        DentistEmail         = co.CreatedBy?.Email ?? "",
        DentistPhone         = co.CreatedBy?.Phone,
        DentistClinicAddress = co.CreatedBy?.AddressPlace,

        LabId = co.AssignedLabId,

        Items = co.Items.Select(i => new OrderDetailsItemDto
        {
            ItemId           = i.Id,
            CompensationType = i.CompensationType.ToString(),
            ToothNumbers     = i.ToothNumbers
        }).ToList(),

        RequiredImages = co.RequiredImages ?? new List<string>(),

        Files = co.Files.Select(f => new FileDto
        {
            Id         = f.Id,
            Path       = f.Path,
            Type       = f.Type.ToString(),
            UploadedAt = f.UploadedAt
        }).ToList()
    };
}
