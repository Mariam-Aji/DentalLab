using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class CaseOrderRepository : ICaseOrderRepository
{
    private readonly ApplicationDbContext _context;
    public CaseOrderRepository(ApplicationDbContext context) => _context = context;

    public async Task<CaseOrder> CreateOrderAsync(CaseOrder order)
    {
        await _context.CaseOrders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<CaseOrder?> GetOrderByIdAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task UpdateOrderAsync(CaseOrder order)
    {
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task AddOrderItemAsync(CaseOrderItem item)
    {
        await _context.CaseOrderItems.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task<LabPrice?> GetUnitPriceAsync(int labId, CompensationType type)
    {
        return await _context.LabPrices
            .FirstOrDefaultAsync(lp => lp.LabId == labId && lp.CompensationType == type);
    }

    public async Task<bool> IsDentistConnectedToLab(int dentistId, int labId)
    {
        return await _context.ConnectionRequests.AnyAsync(cr =>
            cr.FromDentistId == dentistId &&
            cr.ToLabId == labId &&
            cr.Status == ConnectionRequestStatus.Accepted);
    }
}