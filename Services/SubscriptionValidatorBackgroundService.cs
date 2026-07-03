using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DentalLab.Api.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalLab.Api.Services
{
    public class SubscriptionValidatorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionValidatorBackgroundService> _logger;

        public SubscriptionValidatorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionValidatorBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("بدأت خدمة فحص اشتراكات المخابر تلقائياً في الخلفية...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<ILabSubscriptionRepository>();

                        _logger.LogInformation("جاري التحقق من التواريخ الحالية للاشتراكات المنتهية...");

                        var expiredLabs = await repository.GetExpiredLabsAsync();
                        var labsList = expiredLabs.ToList();

                        if (labsList.Any())
                        {
                            _logger.LogWarning($"تم العثور على {labsList.Count} مخبر انتهت فترة اشتراكهم. جاري تحويل حالتهم إلى معلقة/محظورة...");
                            await repository.UpdateLabsRangeAsync(labsList);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "حدث خطأ غير متوقع أثناء فحص الاشتراكات في الخلفية.");
                }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}