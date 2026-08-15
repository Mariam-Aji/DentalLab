//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using DentalLab.Api.Models;
//using DentalLab.Api.Repositories;
//using DentalLab.Api.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace DentalLab.Api.Services
//{
//    public class SubscriptionValidatorBackgroundService : BackgroundService
//    {
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly ILogger<SubscriptionValidatorBackgroundService> _logger;

//        public SubscriptionValidatorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionValidatorBackgroundService> logger)
//        {
//            _scopeFactory = scopeFactory;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("بدأت خدمة فحص اشتراكات المخابر تلقائياً في الخلفية...");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    using (var scope = _scopeFactory.CreateScope())
//                    {
//                        var repository = scope.ServiceProvider.GetRequiredService<ILabSubscriptionRepository>();
//                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

//                        _logger.LogInformation("جاري التحقق من التواريخ الحالية للاشتراكات المنتهية...");

//                        var expiredLabs = await repository.GetExpiredLabsAsync();
//                        var labsList = expiredLabs.ToList();

//                        if (labsList.Any())
//                        {
//                            _logger.LogWarning($"تم العثور على {labsList.Count} مخبر انتهت فترة اشتراكهم. جاري تعليق الحسابات وإرسال الإيميلات...");

//                            foreach (var lab in labsList)
//                            {
//                                if (lab.Owner != null && lab.Owner.Status != AccountStatus.Suspended)
//                                {
//                                    // 1. تعليق الحساب (سيؤدي لإبطال الـ Token تلقائياً بفضل الـ Middleware)
//                                    lab.Owner.Status = AccountStatus.Suspended;

//                                    // 2. إرسال الإيميل لصاحب الحساب
//                                    if (!string.IsNullOrEmpty(lab.Owner.Email))
//                                    {
//                                        var emailSubject = "انتهاء مدة اشتراك المخبر";
//                                        var emailBody = $"مرحباً {lab.Owner.Name}،\n\nنود إعلامك بأن مدة اشتراك المخبر الخاص بك قد انتهت بحسب سجلات السداد، وتم تعليق الحساب مؤقتاً.";

//                                        await emailService.SendEmailAsync(lab.Owner.Email, emailSubject, emailBody);
//                                    }
//                                }
//                            }

//                            // 3. حفظ التعديلات في قاعدة البيانات
//                            await repository.UpdateLabsRangeAsync(labsList);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "حدث خطأ غير متوقع أثناء فحص الاشتراكات في الخلفية.");
//                }

//                // يمكنك جعلها مثلاً كل دقيقة أو ساعة بدلاً من كل ثانية لئلا تستهلك معالج السيرفر
//                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
//            }
//        }
//    }
//}