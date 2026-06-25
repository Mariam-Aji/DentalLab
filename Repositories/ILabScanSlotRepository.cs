using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabScanSlotRepository
{
    /// <summary>كل مواعيد المخبر (محجوزة وغير محجوزة)</summary>
    Task<List<LabScanSlot>> GetAllSlotsAsync(int labId);

    /// <summary>موعد واحد بالـ ID مع التحقق إنه تابع للمخبر</summary>
    Task<LabScanSlot?> GetSlotAsync(int slotId, int labId);

    Task<bool> SlotExistsAsync(int labId, DateTime date, TimeSpan time);

    Task<int> GetBookingsCountAsync(int labId);

    Task AddSlotAsync(LabScanSlot slot);

    Task UpdateSlotAsync(LabScanSlot slot);

    Task DeleteSlotAsync(LabScanSlot slot);

    /// <summary>الحجوزات اللي عملها الدكاترة على مواعيد هذا المخبر</summary>
    Task<List<ScanVisitRequest>> GetBookingsAsync(int labId);
}
