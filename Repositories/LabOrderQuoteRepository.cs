using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabOrderQuoteRepository : ILabOrderQuoteRepository
{
    private readonly ApplicationDbContext _context;

    public LabOrderQuoteRepository(ApplicationDbContext context) => _context = context;

    public async Task<CaseOrder?> GetOrderWithItemsAndPatientAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(x => x.Items)
            .Include(x => x.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }

    public async Task<List<LabPrice>> GetLabPricesAsync(int labId)
    {
        return await _context.LabPrices
            .Where(p => p.LabId == labId)
            .ToListAsync();
    }

    public async Task UpdateOrderAsync(CaseOrder order)
    {
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
    }
}
