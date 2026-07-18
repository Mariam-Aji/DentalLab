using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Models;
using DentalLab.Api.Data;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CaseOrder?> GetOrderWithUserAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(o => o.CreatedBy)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<bool> UpdateOrderPaymentStatusAsync(int orderId, decimal paidAmount, bool isPaid)
    {
        var order = await _context.CaseOrders.FindAsync(orderId);
        if (order == null) return false;

        order.IsPaid = isPaid;
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
        return true;
    }
}