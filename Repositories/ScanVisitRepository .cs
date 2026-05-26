using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ScanVisitRepository : IScanVisitRepository
{
    private readonly ApplicationDbContext _context;

    public ScanVisitRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🔹 جلب المواعيد غير المحجوزة فقط والتي تاريخها من اليوم فصاعداً
    public async Task<List<LabScanSlot>> GetAvailableSlotsAsync(int labId, DateTime fromDate)
    {
        var today = fromDate.Date;

        return await _context.LabScanSlots
            .Where(x => x.LabId == labId
                     && !x.IsBooked
                     && x.Date >= today)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Time)
            .ToListAsync();
    }

    public async Task<LabScanSlot?> GetSlotByIdAsync(int slotId)
    {
        return await _context.LabScanSlots
            .FirstOrDefaultAsync(x => x.Id == slotId);
    }

    public async Task AddSlotAsync(LabScanSlot slot)
    {
        await _context.LabScanSlots.AddAsync(slot);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSlotAsync(LabScanSlot slot)
    {
        _context.LabScanSlots.Update(slot);
        await _context.SaveChangesAsync();
    }

    public async Task AddVisitRequestAsync(ScanVisitRequest request)
    {
        await _context.ScanVisitRequests.AddAsync(request);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetLabOwnerUserAsync(int labId)
    {
        return await _context.Labs
            .Where(x => x.Id == labId)
            .Select(x => x.Owner)
            .FirstOrDefaultAsync();
    }

    // 🔹 الدالة المفقودة التي تحل خطأ الـ Interface وتضيف الإشعار لقاعدة البيانات
    public async Task AddNotificationAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }
}