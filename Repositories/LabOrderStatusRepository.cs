using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabOrderStatusRepository : ILabOrderStatusRepository
{
    private readonly ApplicationDbContext _context;

    public LabOrderStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<(CaseStatus Status, int Count)>> GetOrdersCountByStatusAsync(int labId)
    {
        var raw = await _context.CaseOrders
            .Where(co => co.AssignedLabId == labId)
            .GroupBy(co => co.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return raw.Select(x => (x.Status, x.Count)).ToList();
    }

    public async Task<List<CaseOrder>> GetOrdersByStatusAsync(int labId, CaseStatus status)
    {
        return await _context.CaseOrders
            .Include(co => co.Items)
            .Include(co => co.Files)
            .Include(co => co.CreatedBy)
            .Include(co => co.Patient)
            .Where(co => co.AssignedLabId == labId && co.Status == status)
            .OrderBy(co => co.DeliveryDate == null)
            .ThenBy(co => co.DeliveryDate)
            .ThenBy(co => co.CreatedAt)
            .ToListAsync();
    }

    public async Task<CaseOrder?> GetOrderByIdAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(co => co.Items)
            .Include(co => co.Files)
            .Include(co => co.CreatedBy)
            .FirstOrDefaultAsync(co => co.Id == orderId);
    }

    public async Task<CaseOrder?> GetOrderByIdForLabAsync(int orderId, int labId)
    {
        return await _context.CaseOrders
            .Include(co => co.Items)
            .Include(co => co.Files)
            .Include(co => co.CreatedBy)
            .FirstOrDefaultAsync(co => co.Id == orderId && co.AssignedLabId == labId);
    }

    public async Task SaveOrderAsync(CaseOrder order)
    {
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task AddFileAsync(FileResource file)
    {
        await _context.FileResources.AddAsync(file);
        await _context.SaveChangesAsync();
    }
}
