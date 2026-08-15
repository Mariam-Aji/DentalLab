using System;
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

    public async Task<CaseOrder?> GetOrderWithUserAndLabAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(o => o.CreatedBy)     
            .Include(o => o.AssignedLab)    
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<CaseOrder?> UpdateOrderPaymentStatusAsync(int orderId, decimal paidAmount, bool isPaid)
    {
        var order = await _context.CaseOrders
            .Include(o => o.CreatedBy)                     
            .Include(o => o.AssignedLab)                  
                .ThenInclude(l => l!.Owner)               
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        order.IsPaid = isPaid;
        if (isPaid)
        {
            order.PaidAt = DateTime.UtcNow;
        }

        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Advertisement?> GetAdvertisementWithUserAsync(int adId)
    {
        return await _context.Advertisements
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == adId);
    }

    public async Task<Advertisement?> UpdateAdPaymentStatusAsync(int adId, decimal paidAmount, bool isPaid)
    {
        var ad = await _context.Advertisements
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == adId);

        if (ad == null) return null;

        ad.IsPaid = isPaid;

        if (isPaid)
        {
            ad.PaidAt = DateTime.UtcNow;
            ad.IsActive = true;
        }

        _context.Advertisements.Update(ad);
        await _context.SaveChangesAsync();
        return ad;
    }
}