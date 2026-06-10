using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabOrderRepository : ILabOrderRepository
{
    private readonly ApplicationDbContext _context;

    public LabOrderRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<CaseOrder>> GetPendingOrdersForLabAsync(int labId)
    {
        return await _context.CaseOrders
            .Include(co => co.Items)
            .Include(co => co.Files)
            .Include(co => co.CreatedBy)
            .Where(co => co.AssignedLabId == labId && (co.Status == CaseStatus.Pennding))
            .OrderByDescending(co => co.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingOrdersCountForLabAsync(int labId)
    {
        return await _context.CaseOrders
            .CountAsync(co => co.AssignedLabId == labId && co.Status == CaseStatus.Pennding);
    }

    public async Task<CaseOrder?> GetOrderWithFilesAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(x => x.Items)
            .Include(x => x.Files)
            .Include(x => x.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }

    public async Task UpdateOrderAsync(CaseOrder order)
    {
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
    }
}
