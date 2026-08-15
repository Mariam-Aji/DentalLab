using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class LabSubscriptionService : ILabSubscriptionService
    {
        private readonly ILabSubscriptionRepository _subscriptionRepository;
        private readonly IEmailService _emailService; 
        public LabSubscriptionService(
            ILabSubscriptionRepository subscriptionRepository,
            IEmailService emailService) 
        {
            _subscriptionRepository = subscriptionRepository;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message)> CreateLabSubscriptionAsync(int labId, CreateSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");
            if (lab.Owner == null) return (false, "المستخدم المرتبط بهذا المخبر غير موجود.");

            if (lab.SubscriptionPayments != null && lab.SubscriptionPayments.Any())
            {
                return (false, "هذا المشترك موجود مسبقاً في النظام. يرجى استخدام خيار (تجديد الاشتراك) بدلاً من تسجيل اشتراك جديد.");
            }

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

            if (!string.IsNullOrEmpty(lab.Owner.Email))
            {
                var emailSubject = "تفعيل حساب المخبر الخاص بك";
                var emailBody = $"مرحباً {lab.Owner.Name}،\n\nتم تسجيل اشتراك المخبر الخاص بك وتفعيل حسابك بنجاح في النظام.\nفترة الاشتراك: من {dto.PeriodStartUtc:yyyy-MM-dd} إلى {dto.PeriodEndUtc:yyyy-MM-dd}.\n\nشكراً لاستخدامك خدماتنا.";

                await _emailService.SendEmailAsync(lab.Owner.Email, emailSubject, emailBody);
            }

            return (true, "تم تسجيل الاشتراك الأول وتفعيل حساب المخبر بنجاح.");
        }

        public async Task<IEnumerable<ActiveLabDto>> GetActiveSubscribedLabsAsync()
        {
            var labs = await _subscriptionRepository.GetActiveSubscribedLabsAsync();
            var now = DateTime.UtcNow;

            var activeLabsList = new List<ActiveLabDto>();

            foreach (var lab in labs)
            {
                var latestPayment = lab.SubscriptionPayments
                    .OrderByDescending(p => p.PaidAtUtc)
                    .FirstOrDefault();

                if (latestPayment != null && latestPayment.PeriodEndUtc > now)
                {
                    activeLabsList.Add(new ActiveLabDto
                    {
                        LabId = lab.Id,
                        LabName = lab.Owner?.Name ?? "مخبر غير مسمى",
                        Email = lab.Owner?.Email ?? string.Empty,
                        SubscriptionStartUtc = latestPayment.PeriodStartUtc,
                        SubscriptionEndUtc = latestPayment.PeriodEndUtc,
                        RemainingDays = (latestPayment.PeriodEndUtc - now).Days
                    });
                }
            }

            return activeLabsList;
        }
        public async Task<(bool Success, string Message)> UpdateSubscriptionInfoAsync(int labId, UpdateSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");

            var latestPayment = await _subscriptionRepository.GetLatestPaymentAsync(labId);
            if (latestPayment == null) return (false, "لا يوجد سجل اشتراك سابق لتعديله.");

            latestPayment.Amount = dto.Amount;
            latestPayment.PeriodStartUtc = dto.PeriodStartUtc;
            latestPayment.PeriodEndUtc = dto.PeriodEndUtc;

            lab.SubscriptionStartUtc = dto.PeriodStartUtc;
            lab.SubscriptionEndUtc = dto.PeriodEndUtc;

            await _subscriptionRepository.UpdateSubscriptionPaymentAsync(latestPayment);
            await _subscriptionRepository.UpdateLabAndUserAsync(lab, lab.Owner);

            return (true, "تم تعديل معلومات الاشتراك بنجاح.");
        }

        public async Task<(bool Success, string Message)> RenewSubscriptionAsync(int labId, RenewSubscriptionDto dto)
        {
            var lab = await _subscriptionRepository.GetLabWithUserAsync(labId);
            if (lab == null) return (false, "المخبر المحدد غير موجود.");
            if (lab.Owner == null) return (false, "المستخدم المرتبط بهذا المخبر غير موجود.");

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

            lab.SubscriptionStartUtc = dto.PeriodStartUtc;
            lab.SubscriptionEndUtc = dto.PeriodEndUtc;

            lab.Owner.Status = AccountStatus.Active;

            await _subscriptionRepository.AddSubscriptionPaymentAsync(newRenewalPayment);
            await _subscriptionRepository.UpdateLabAndUserAsync(lab, lab.Owner);

            if (!string.IsNullOrEmpty(lab.Owner.Email))
            {
                var emailSubject = "تجديد اشتراك وتفعيل حساب المخبر";
                var emailBody = $"مرحباً {lab.Owner.Name}،\n\nتم تجديد اشتراك المخبر الخاص بك وإعادة تفعيل الحساب بنجاح.\nالفترة الجديدة: من {dto.PeriodStartUtc:yyyy-MM-dd} إلى {dto.PeriodEndUtc:yyyy-MM-dd}.\n\nشكراً لاستخدامك خدماتنا.";

                await _emailService.SendEmailAsync(lab.Owner.Email, emailSubject, emailBody);
            }

            return (true, "تم تجديد الاشتراك وشحن الحساب بنجاح، وإعادة تفعيل صلاحيات المخبر.");
        }

        public async Task<IEnumerable<ActiveLabDto>> GetExpiredLabsAsync()
        {
            var expiredLabs = await _subscriptionRepository.GetExpiredLabsAsync();
            var now = DateTime.UtcNow;

            return expiredLabs.Select(l =>
            {
                var latestPayment = l.SubscriptionPayments?
                    .OrderByDescending(p => p.PaidAtUtc)
                    .FirstOrDefault();

                return new ActiveLabDto
                {
                    LabId = l.Id,
                    LabName = l.Owner?.Name ?? "مخبر غير مسمى",
                    Email = l.Owner?.Email ?? string.Empty,
                    SubscriptionStartUtc = latestPayment?.PeriodStartUtc ?? l.SubscriptionStartUtc,
                    SubscriptionEndUtc = latestPayment?.PeriodEndUtc ?? l.SubscriptionEndUtc ?? now,
                    RemainingDays = latestPayment != null ? (latestPayment.PeriodEndUtc - now).Days : (l.SubscriptionEndUtc.HasValue ? (l.SubscriptionEndUtc.Value - now).Days : 0)
                };
            });
        }
        public async Task<bool> UpdateAllSubscriptionsAmountAsync(decimal newAmount)
        {
            try
            {
                int affectedRows = await _subscriptionRepository.UpdateAllSubscriptionAmountsAsync(newAmount);
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}