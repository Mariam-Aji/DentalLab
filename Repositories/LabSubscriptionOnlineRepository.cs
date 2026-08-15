using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabSubscriptionOnlineRepository : ILabSubscriptionOnlineRepository
{
    private readonly ApplicationDbContext _context;

    public LabSubscriptionOnlineRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Lab?> GetLabWithPaymentsByUserIdAsync(int userId)
    {
        return await _context.Labs
            .Include(l => l.Owner)
            .Include(l => l.SubscriptionPayments)
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }

    public async Task<Lab?> GetLabWithPaymentsByLabIdAsync(int labId)
    {
        return await _context.Labs
            .Include(l => l.Owner)
            .Include(l => l.SubscriptionPayments)
            .FirstOrDefaultAsync(l => l.Id == labId);
    }

    public async Task<LabSubscriptionPayment?> GetFreeTrialPaymentAsync(int labId)
    {
        return await _context.LabSubscriptionPayments
            .Where(p => p.LabId == labId
                     && p.Reference != null
                     && p.Reference.Contains("Free Trial"))
            .FirstOrDefaultAsync();
    }

    public async Task<LabSubscriptionPayment?> GetLatestPaymentByLabIdAsync(int labId)
    {
        return await _context.LabSubscriptionPayments
            .Where(p => p.LabId == labId)
            .OrderByDescending(p => p.PaidAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task AddSubscriptionPaymentAsync(LabSubscriptionPayment payment)
    {
        await _context.LabSubscriptionPayments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLabAndUserAsync(Lab lab, User user)
    {
        _context.Labs.Update(lab);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
