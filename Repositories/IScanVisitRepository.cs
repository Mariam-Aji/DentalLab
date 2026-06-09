using DentalLab.Api.Models;

public interface IScanVisitRepository
{
    Task<List<LabScanSlot>> GetAvailableSlotsAsync(int labId, DateTime fromDate);

    Task<LabScanSlot?> GetSlotByIdAsync(int slotId);

    Task AddSlotAsync(LabScanSlot slot);

    Task UpdateSlotAsync(LabScanSlot slot);

    Task AddVisitRequestAsync(ScanVisitRequest request);

    Task<User?> GetLabOwnerUserAsync(int labId);
    Task AddNotificationAsync(Notification notification);
    //
}