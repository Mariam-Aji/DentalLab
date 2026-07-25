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
            .Include(o => o.CreatedBy)      // الطبيب الدافع
            .Include(o => o.AssignedLab)    // المخبر المستلم
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
    public async Task<Advertisement?> GetAdvertisementWithUserAsync(int adId)
    {
        return await _context.Advertisements
            .Include(a => a.User) // جلب بيانات المستخدم الناشر (طبيب، مخبر، أو عميل إعلانات)
            .FirstOrDefaultAsync(a => a.Id == adId);
    }

    public async Task<bool> UpdateAdPaymentStatusAsync(int adId, decimal paidAmount, bool isPaid)
    {
        var ad = await _context.Advertisements.FindAsync(adId);
        if (ad == null) return false;

        ad.IsPaid = isPaid; // تحديث حقل الدفع في الإعلان
        _context.Advertisements.Update(ad);
        await _context.SaveChangesAsync();
        return true;
    }
}