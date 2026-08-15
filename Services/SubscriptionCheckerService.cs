using DentalLab.Api.Data;
using DentalLab.Api.Models;
using DentalLab.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services
{
    public class SubscriptionCheckerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionCheckerService> _logger;

        public SubscriptionCheckerService(IServiceProvider serviceProvider, ILogger<SubscriptionCheckerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var checkInterval = TimeSpan.FromSeconds(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        var now = DateTime.UtcNow;

                        var labs = await context.Labs
                            .Include(l => l.Owner)
                            .Include(l => l.SubscriptionPayments)
                            .Where(l => l.Owner != null && l.Owner.Status == AccountStatus.Active)
                            .ToListAsync();

                        foreach (var lab in labs)
                        {
                            var latestPayment = lab.SubscriptionPayments
                                .OrderByDescending(p => p.PaidAtUtc)
                                .FirstOrDefault();

                            if (latestPayment != null && latestPayment.PeriodEndUtc <= now)
                            {
                                // 1. تحويل حالة الحساب إلى بانتظار الدفع
                                lab.Owner.Status = AccountStatus.PendingPayment;

                                context.Entry(lab.Owner).State = EntityState.Modified;

                                if (!string.IsNullOrEmpty(lab.Owner.Email))
                                {
                                    var emailSubject = "انتهاء مدة اشتراك المخبر";
                                    var emailBody = $"مرحباً {lab.Owner.Name}،\n\nنود إعلامك بأن مدة اشتراك المخبر الخاص بك قد انتهت بحسب سجلات السداد، وتم تحويل حالة الحساب إلى بانتظار الدفع وفصل الجلسة الحالية.";

                                    await emailService.SendEmailAsync(lab.Owner.Email, emailSubject, emailBody);
                                }
                            }
                        }

                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "حدث خطأ أثناء التحقق من الاشتراكات في الخلفية.");
                }

                await Task.Delay(checkInterval, stoppingToken);
            }
        }
    }
}