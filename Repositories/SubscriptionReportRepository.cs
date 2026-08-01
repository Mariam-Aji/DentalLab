using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Data;
using DentalLab.Api.Dtos.Reports;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Repositories
{
    public class SubscriptionReportRepository : ISubscriptionReportRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب المخابر التي ستنتهي خلال N من الأيام
        public async Task<List<ExpiringLabDto>> GetExpiringSoonLabsAsync(int daysThreshold)
        {
            var now = DateTime.UtcNow;
            var targetDate = now.AddDays(daysThreshold);

            return await _context.Labs
                .AsNoTracking()
                .Include(l => l.Owner)
                .Where(l => l.SubscriptionEndUtc.HasValue &&
                            l.SubscriptionEndUtc.Value > now &&
                            l.SubscriptionEndUtc.Value <= targetDate)
                .Select(l => new ExpiringLabDto
                {
                    LabId = l.Id,
                    LabName = l.Owner.Name ?? "غير مسمى",
                    OwnerEmail = l.Owner.Email ?? string.Empty,
                    SubscriptionEndUtc = l.SubscriptionEndUtc.Value,
                    DaysRemaining = (l.SubscriptionEndUtc.Value - now).Days
                })
                .OrderBy(l => l.SubscriptionEndUtc)
                .ToListAsync();
        }

        // 2. إحصائيات توزيع الحسابات Active vs Suspended
        public async Task<LabsStatusDistributionDto> GetLabsStatusDistributionAsync()
        {
            var activeCount = await _context.Labs
                .AsNoTracking()
                .CountAsync(l => l.Owner.Status == AccountStatus.Active);

            var suspendedCount = await _context.Labs
                .AsNoTracking()
                .CountAsync(l => l.Owner.Status == AccountStatus.Suspended);

            return new LabsStatusDistributionDto
            {
                ActiveLabsCount = activeCount,
                SuspendedLabsCount = suspendedCount,
                TotalLabsCount = activeCount + suspendedCount
            };
        }

        // 3. حساب معدل الاحتفاظ بالعملاء (Retention Rate)
        public async Task<RetentionRateReportDto> GetRetentionRateStatsAsync()
        {
            // المخابر التي لديها عمليات دفع
            var labsWithPayments = _context.Labs
                .AsNoTracking()
                .Where(l => l.SubscriptionPayments.Any());

            var totalSubscribedLabs = await labsWithPayments.CountAsync();

            // المخابر التي قامت بالدفع/التجديد أكثر من مرة واحدة (Count > 1)
            var renewedLabsCount = await labsWithPayments
                .CountAsync(l => l.SubscriptionPayments.Count > 1);

            double retentionRate = totalSubscribedLabs > 0
                ? Math.Round((double)renewedLabsCount / totalSubscribedLabs * 100, 2)
                : 0;

            return new RetentionRateReportDto
            {
                TotalSubscribedLabs = totalSubscribedLabs,
                RenewedLabsCount = renewedLabsCount,
                RetentionRatePercentage = retentionRate
            };
        }
    }
}