using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabScanSlotService
{
    Task<(List<LabScanSlotResponseDto>? slots, string? error)> GetAllSlotsAsync(int userId);

    Task<(LabScanSlotResponseDto? slot, string? error)> AddSlotAsync(int userId, UpsertScanSlotDto dto);

    Task<(LabScanSlotResponseDto? slot, string? error)> UpdateSlotAsync(int userId, int slotId, UpsertScanSlotDto dto);

    Task<string?> DeleteSlotAsync(int userId, int slotId);

    Task<(List<ScanVisitBookingDto>? bookings, string? error)> GetBookingsAsync(int userId);

    Task<(int count, string? error)> GetBookingsCountAsync(int userId);
}
