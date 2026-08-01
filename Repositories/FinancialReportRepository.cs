using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Data;
using DentalLab.Api.Repositories.Interfaces;

namespace DentalLab.Api.Repositories
{
    public class FinancialReportRepository : IFinancialReportRepository
    {
        private readonly ApplicationDbContext _context;

        public FinancialReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal TotalRevenue, int Count)> GetPaidAdvertisementsStatsAsync()
        {
            // جلب الإعلانات المدفوعة فقط
            var paidAdsQuery = _context.Advertisements
                .AsNoTracking()
                .Where(a => a.IsPaid && a.Price.HasValue);

            var totalRevenue = await paidAdsQuery.SumAsync(a => a.Price.Value);
            var count = await paidAdsQuery.CountAsync();

            return (totalRevenue, count);
        }

        public async Task<(decimal TotalRevenue, int Count)> GetActiveSubscriptionsStatsAsync()
        {
            var now = DateTime.UtcNow;

            // جلب الاشتراكات النشطة فقط (التي لم ينتهِ تاريخها بعد)
            var activeSubscriptionsQuery = _context.LabSubscriptionPayments
                .AsNoTracking()
                .Where(p => p.PeriodEndUtc >= now);

            var totalRevenue = await activeSubscriptionsQuery.SumAsync(p => p.Amount);
            var count = await activeSubscriptionsQuery.CountAsync();

            return (totalRevenue, count);
        }
    }
}