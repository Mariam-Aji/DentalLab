using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabScanSlotRepository : ILabScanSlotRepository
{
    private readonly ApplicationDbContext _context;

    public LabScanSlotRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<LabScanSlot>> GetAllSlotsAsync(int labId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.LabScanSlots
            .Where(s => s.LabId == labId && s.Date >= today)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Time)
            .ToListAsync();
    }

    public async Task<LabScanSlot?> GetSlotAsync(int slotId, int labId)
    {
        return await _context.LabScanSlots
            .FirstOrDefaultAsync(s => s.Id == slotId && s.LabId == labId);
    }

    public async Task<bool> SlotExistsAsync(int labId, DateTime date, TimeSpan time)
    {
        return await _context.LabScanSlots
            .AnyAsync(s => s.LabId == labId && s.Date == date.Date && s.Time == time);
    }

    public async Task<int> GetBookingsCountAsync(int labId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.ScanVisitRequests
            .Include(r => r.Slot)
            .CountAsync(r => r.LabId == labId && r.Slot!.Date >= today);
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

    public async Task DeleteSlotAsync(LabScanSlot slot)
    {
        _context.LabScanSlots.Remove(slot);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ScanVisitRequest>> GetBookingsAsync(int labId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.ScanVisitRequests
            .Include(r => r.Slot)
            .Include(r => r.Dentist)
            .Where(r => r.LabId == labId && r.Slot!.Date >= today)
            .OrderBy(r => r.Slot!.Date)
            .ThenBy(r => r.Slot!.Time)
            .ToListAsync();
    }
}
