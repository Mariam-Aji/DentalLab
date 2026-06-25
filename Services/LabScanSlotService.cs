using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabScanSlotService : ILabScanSlotService
{
    private readonly ILabScanSlotRepository _repo;
    private readonly ILabProfileService     _labProfile;

    public LabScanSlotService(ILabScanSlotRepository repo, ILabProfileService labProfile)
    {
        _repo       = repo;
        _labProfile = labProfile;
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async Task<(Lab? lab, string? error)> GetLabWithScanCheckAsync(int userId)
    {
        var lab = await _labProfile.GetProfileAsync(userId);
        if (lab == null) return (null, "المخبر غير موجود.");
        if (!lab.HasScanVisitService) return (null, "خدمة المسح غير مفعّلة لهذا المخبر.");
        return (lab, null);
    }

    private static LabScanSlotResponseDto ToDto(LabScanSlot s) => new()
    {
        Id        = s.Id,
        Date      = s.Date,
        Time      = s.Time,
        Period    = s.Period.ToString(),
        IsBooked  = s.IsBooked,
        CreatedAt = s.CreatedAt,
    };

    // ─── operations ────────────────────────────────────────────────────────────

    public async Task<(List<LabScanSlotResponseDto>? slots, string? error)> GetAllSlotsAsync(int userId)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return (null, error);

        var slots = await _repo.GetAllSlotsAsync(lab!.Id);
        return (slots.Select(ToDto).ToList(), null);
    }

    public async Task<(LabScanSlotResponseDto? slot, string? error)> AddSlotAsync(int userId, UpsertScanSlotDto dto)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return (null, error);

        if (!Enum.TryParse<SlotPeriod>(dto.Period, ignoreCase: true, out var period))
            return (null, "قيمة Period غير صحيحة، استخدم AM أو PM.");

        if (dto.Date.Date < DateTime.UtcNow.Date)
            return (null, "لا يمكن إضافة موعد في تاريخ ماضٍ.");

        var duplicate = await _repo.SlotExistsAsync(lab!.Id, dto.Date, dto.Time);
        if (duplicate)
            return (null, "يوجد موعد بنفس التاريخ والوقت مسبقاً.");

        var slot = new LabScanSlot
        {
            LabId  = lab!.Id,
            Date   = dto.Date.Date,
            Time   = dto.Time,
            Period = period,
        };

        await _repo.AddSlotAsync(slot);
        return (ToDto(slot), null);
    }

    public async Task<(LabScanSlotResponseDto? slot, string? error)> UpdateSlotAsync(int userId, int slotId, UpsertScanSlotDto dto)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return (null, error);

        var slot = await _repo.GetSlotAsync(slotId, lab!.Id);
        if (slot == null) return (null, "الموعد غير موجود.");

        if (slot.IsBooked)
            return (null, "لا يمكن تعديل موعد محجوز مسبقاً.");

        if (!Enum.TryParse<SlotPeriod>(dto.Period, ignoreCase: true, out var period))
            return (null, "قيمة Period غير صحيحة، استخدم AM أو PM.");

        if (dto.Date.Date < DateTime.UtcNow.Date)
            return (null, "لا يمكن تعيين موعد في تاريخ ماضٍ.");

        // نتحقق من التكرار مع استثناء الـ slot الحالي نفسه
        var duplicate = await _repo.SlotExistsAsync(lab!.Id, dto.Date, dto.Time);
        if (duplicate && (slot.Date != dto.Date.Date || slot.Time != dto.Time))
            return (null, "يوجد موعد بنفس التاريخ والوقت مسبقاً.");

        slot.Date   = dto.Date.Date;
        slot.Time   = dto.Time;
        slot.Period = period;

        await _repo.UpdateSlotAsync(slot);
        return (ToDto(slot), null);
    }

    public async Task<string?> DeleteSlotAsync(int userId, int slotId)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return error;

        var slot = await _repo.GetSlotAsync(slotId, lab!.Id);
        if (slot == null) return "الموعد غير موجود.";

        if (slot.IsBooked)
            return "لا يمكن حذف موعد محجوز مسبقاً.";

        await _repo.DeleteSlotAsync(slot);
        return null;
    }

    public async Task<(List<ScanVisitBookingDto>? bookings, string? error)> GetBookingsAsync(int userId)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return (null, error);

        var bookings = await _repo.GetBookingsAsync(lab!.Id);

        var result = bookings.Select(r => new ScanVisitBookingDto
        {
            BookingId   = r.Id,
            SlotId      = r.LabScanSlotId,
            Date        = r.Slot!.Date,
            Time        = r.Slot.Time,
            Period      = r.Slot.Period.ToString(),
            DentistId    = r.DentistId,
            DentistName  = r.Dentist?.Name ?? "",
            DentistEmail = r.Dentist?.Email ?? "",
            DentistPhone = r.Dentist?.Phone,
            ClinicName    = r.Dentist?.NamePlace,
            ClinicAddress = r.Dentist?.AddressPlace,
            ClinicCity    = r.Dentist?.CityPlace,
            ClinicCountry = r.Dentist?.CountryPlace,
            BookedAt      = r.CreatedAt,
        }).ToList();

        return (result, null);
    }

    public async Task<(int count, string? error)> GetBookingsCountAsync(int userId)
    {
        var (lab, error) = await GetLabWithScanCheckAsync(userId);
        if (error != null) return (0, error);

        var count = await _repo.GetBookingsCountAsync(lab!.Id);
        return (count, null);
    }
}
