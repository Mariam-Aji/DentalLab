using System;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class LabSubscriptionService : ILabSubscriptionService
    {
        private readonly ILabSubscriptionRepository _subscriptionRepository;

        public LabSubscriptionService(ILabSubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<(bool Success, string Message)> CreateLabSubscriptionAsync(int labId, CreateSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");
            if (lab.Owner == null) return (false, "المستخدم المرتبط بهذا المخبر غير موجود.");

            // 🌟 الفحص: منع تسجيل المشترك مسبقاً وتوجيهه للتجديد
            if (lab.SubscriptionPayments != null && lab.SubscriptionPayments.Any())
            {
                return (false, "هذا المشترك موجود مسبقاً في النظام. يرجى استخدام خيار (تجديد الاشتراك) بدلاً من تسجيل اشتراك جديد.");
            }

            // بناء كائن الدفع الجديد للاشتراك لأول مرة
            var subscriptionPayment = new LabSubscriptionPayment
            {
                LabId = labId,
                Amount = dto.Amount,
                Method = SubscriptionPaymentMethod.Manual,
                PaidAtUtc = DateTime.UtcNow,
                PeriodStartUtc = dto.PeriodStartUtc,
                PeriodEndUtc = dto.PeriodEndUtc,
                Reference = "Activated by Admin"
            };

            lab.SubscriptionStartUtc = dto.PeriodStartUtc;
            lab.SubscriptionEndUtc = dto.PeriodEndUtc;

            lab.Owner.Status = AccountStatus.Active;

            await _subscriptionRepository.AddSubscriptionPaymentAsync(subscriptionPayment);
            await _subscriptionRepository.UpdateLabAndUserAsync(lab, lab.Owner);

            return (true, "تم تسجيل الاشتراك الأول وتفعيل حساب المخبر بنجاح.");
        }
        public async Task<IEnumerable<ActiveLabDto>> GetActiveSubscribedLabsAsync()
        {
            var labs = await _subscriptionRepository.GetActiveSubscribedLabsAsync();
            var now = DateTime.UtcNow;

            var activeLabsList = new List<Lab>();
            var expiredLabsList = new List<Lab>();

            // قائمة مساعدة لحفظ تواريخ أحدث الدفعات لكل مخبر لاستخدامها أثناء الـ Mapping
            var labLatestPayments = new Dictionary<int, LabSubscriptionPayment>();

            foreach (var lab in labs)
            {
                // 🌟 جلب أحدث دفعة اشتراك لهذا المخبر من جدول الـ Payments
                var latestPayment = lab.SubscriptionPayments
                    .OrderByDescending(p => p.PaidAtUtc)
                    .FirstOrDefault();

                if (latestPayment != null)
                {
                    // حفظ الدفعة للرجوع إليها لاحقاً
                    labLatestPayments[lab.Id] = latestPayment;

                    // الفحص بناءً على تاريخ انتهاء الدفعة المسجلة في جدول المدفوعات
                    if (latestPayment.PeriodEndUtc <= now)
                    {
                        if (lab.Owner != null)
                        {
                            lab.Owner.Status = AccountStatus.Suspended;

                            var expiryNotification = new Notification
                            {
                                RecipientId = lab.UserId,
                                Type = NotificationType.StatusChanged,
                                Message = $"مرحباً {lab.Owner.Name}، نود إعلامك بأن مدة اشتراك المخبر الخاص بك قد انتهت بحسب سجلات السداد، وتم تعليق الحساب مؤقتاً.",
                                CreatedAt = now,
                                IsRead = false
                            };

                            lab.Owner.Notifications.Add(expiryNotification);
                        }
                        expiredLabsList.Add(lab);
                    }
                    else
                    {
                        activeLabsList.Add(lab);
                    }
                }
                else
                {
                    // إذا لم يمتلك المخبر أي دفعة سابقة في الجدول، نعتبره منتهياً أو غير مشترك
                    expiredLabsList.Add(lab);
                }
            }

            // تحديث الحسابات المنتهية في قاعدة البيانات
            if (expiredLabsList.Any())
            {
                await _subscriptionRepository.UpdateLabsRangeAsync(expiredLabsList);
            }

            // إرجاع المخابر النشطة مع تعبئة البيانات من جدول الـ Payments مباشرة
            return activeLabsList.Select(l =>
            {
                var payment = labLatestPayments[l.Id];
                return new ActiveLabDto
                {
                    LabId = l.Id,
                    LabName = l.Owner?.Name ?? "مخبر غير مسمى",
                    Email = l.Owner?.Email ?? string.Empty,
                    SubscriptionStartUtc = payment.PeriodStartUtc, // القراءة من جدول المدفوعات
                    SubscriptionEndUtc = payment.PeriodEndUtc,     // القراءة من جدول المدفوعات
                    RemainingDays = (payment.PeriodEndUtc - now).Days
                };
            });
        }
        // 1️⃣ تابع تعديل معلومات الاشتراك الحالي
        public async Task<(bool Success, string Message)> UpdateSubscriptionInfoAsync(int labId, UpdateSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");

            // جلب أحدث دفعة لتعديل بياناتها
            var latestPayment = await _subscriptionRepository.GetLatestPaymentAsync(labId);
            if (latestPayment == null) return (false, "لا يوجد سجل اشتراك سابق لتعديله.");

            // تحديث بيانات الدفعة المالية
            latestPayment.Amount = dto.Amount;
            latestPayment.PeriodStartUtc = dto.PeriodStartUtc;
            latestPayment.PeriodEndUtc = dto.PeriodEndUtc;

            // تحديث البيانات الأساسية في جدول المخبر أيضاً لتبقى متطابقة
            lab.SubscriptionStartUtc = dto.PeriodStartUtc;
            lab.SubscriptionEndUtc = dto.PeriodEndUtc;

            await _subscriptionRepository.UpdateSubscriptionPaymentAsync(latestPayment);
            await _subscriptionRepository.UpdateLabAndUserAsync(lab, lab.Owner);

            return (true, "تم تعديل معلومات الاشتراك بنجاح.");
        }

        // 2️⃣ تابع تجديد الاشتراك (شحن مالي وتمديد فترة)
        public async Task<(bool Success, string Message)> RenewSubscriptionAsync(int labId, RenewSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");
            if (lab.Owner == null) return (false, "المستخدم المرتبط بهذا المخبر غير موجود.");

            // إنشاء سجل دفع جديد تماماً في جدول الاشتراكات والمدفوعات
            var newRenewalPayment = new LabSubscriptionPayment
            {
                LabId = labId,
                Amount = dto.Amount,
                Method = SubscriptionPaymentMethod.Manual,
                PaidAtUtc = DateTime.UtcNow,
                PeriodStartUtc = dto.PeriodStartUtc,
                PeriodEndUtc = dto.PeriodEndUtc,
                Reference = "Renewed by Admin"
            };

            // تحديث حقول الصلاحية في جدول المخبر الأساسي بناءً على التجديد الجديد
            lab.SubscriptionStartUtc = dto.PeriodStartUtc;
            lab.SubscriptionEndUtc = dto.PeriodEndUtc;

            // إعادة تفعيل الحساب تلقائياً في حال كان معلقاً بسبب انتهاء الاشتراك السابق
            lab.Owner.Status = AccountStatus.Active;

            // حفظ السجل المالي الجديد وتحديث حالة المخبر والمستخدم
            await _subscriptionRepository.AddSubscriptionPaymentAsync(newRenewalPayment);
            await _subscriptionRepository.UpdateLabAndUserAsync(lab, lab.Owner);

            return (true, "تم تجديد الاشتراك وشحن الحساب بنجاح، وإعادة تفعيل صلاحيات المخبر.");
        }
    }
}
