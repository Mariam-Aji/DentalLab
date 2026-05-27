using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

public class ScanVisitService : IScanVisitService
{
    private readonly IScanVisitRepository _repository;
    private readonly DentalLab.Api.Data.ApplicationDbContext _context; 

    public ScanVisitService(IScanVisitRepository repository, DentalLab.Api.Data.ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<ScanVisitSlotDto>> GetAvailableSlotsAsync(int labId, DateTime fromDate)
    {
        var slots = await _repository.GetAvailableSlotsAsync(labId, fromDate);

        return slots.Select(s => new ScanVisitSlotDto
        {
            Id = s.Id,
            AppointmentDate = s.Date,
            AppointmentTime = s.Time
        }).ToList();
    }

    public async Task AddSlotAsync(int labId, DateTime date, TimeSpan time, SlotPeriod period)
    {
        var slot = new LabScanSlot
        {
            LabId = labId,
            Date = date.Date,
            Time = time,
            Period = period,
            IsBooked = false
        };
        await _repository.AddSlotAsync(slot);
    }

    public async Task<bool> BookSlotAsync(int dentistId, int labId, int slotId)
    {
        var slot = await _repository.GetSlotByIdAsync(slotId);
        if (slot == null || slot.IsBooked || slot.LabId != labId)
        {
            return false;
        }

        slot.IsBooked = true;
        await _repository.UpdateSlotAsync(slot);

        var request = new ScanVisitRequest
        {
            LabId = labId,
            DentistId = dentistId,
            LabScanSlotId = slotId,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddVisitRequestAsync(request);

      
        var labOwner = await _repository.GetLabOwnerUserAsync(labId);
        if (labOwner != null)
        {
            var notification = new Notification
            {
                RecipientId = labOwner.Id,
                Type = NotificationType.ScanVisitConfirmed, 
                Message = $"تم حجز موعد زيارة فحص جديدة للمخبر الخاص بك بتاريخ {slot.Date:yyyy-MM-dd} في تمام الساعة {slot.Time:hh\\:mm} {slot.Period}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        return true;
    }
   
    public async Task<List<Notification>> GetLabNotificationsAsync(int labOwnerId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == labOwnerId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}