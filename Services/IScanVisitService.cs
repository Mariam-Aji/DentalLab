using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

public interface IScanVisitService
{
    Task<List<ScanVisitSlotDto>> GetAvailableSlotsAsync(int labId, DateTime fromDate);
    Task AddSlotAsync(int labId, DateTime date, TimeSpan time, SlotPeriod period);
    Task<bool> BookSlotAsync(int dentistId, int labId, int slotId);
    Task<List<Notification>> GetLabNotificationsAsync(int labOwnerId);
}