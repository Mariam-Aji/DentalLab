using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class AdminAccountService : IAdminAccountService
    {
        private readonly IAdminAccountsRepository _repo;
        private readonly IAccountsRepository _accountsRepo;
        private readonly IEmailService _emailService; // 🌟 إضافة خدمة البريد الإلكتروني

        public AdminAccountService(
            IAdminAccountsRepository repo,
            IAccountsRepository accountsRepo,
            IEmailService emailService) // 🌟 حقن الخدمة في Constructor
        {
            _repo = repo;
            _accountsRepo = accountsRepo;
            _emailService = emailService;
        }

        public Task<List<User>> GetPendingDentistApprovalsAsync()
            => _repo.GetPendingDentistApprovalsAsync();

        public Task<List<User>> GetPendingLabApprovalsAsync()
            => _repo.GetPendingLabApprovalsAsync();

        public Task<string?> ApproveDentistAsync(int id)
            => SetDentistStatusAsync(id, AccountStatus.Active, requirePendingAdminApproval: true);

        public Task<string?> RejectDentistAsync(int id)
            => SetDentistStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: true);

        public Task<string?> SuspendDentistAsync(int id)
            => SetDentistStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: false);

        public async Task<string?> ApproveLabAsync(int id, decimal amount)
        {
            // 1. تغيير حالة الحساب أولاً
            var error = await SetLabStatusAsync(id, AccountStatus.Active, requirePendingAdminApproval: true);
            if (error != null) return error;

            var user = await _repo.GetUserByIdTrackingAsync(id);
            var labId = await _accountsRepo.GetLabIdByUserIdAsync(id);

            if (user != null && labId.HasValue)
            {
                var lab = await _accountsRepo.GetLabByIdTrackingAsync(labId.Value);

                if (lab != null)
                {
                    var now = DateTime.UtcNow;
                    var periodEnd = now.AddMonths(1); // فترة تجريبية / بدائية مدتها شهر

                    var subscriptionPayment = new LabSubscriptionPayment
                    {
                        Amount = amount,
                        Method = SubscriptionPaymentMethod.Manual,
                        PaidAtUtc = now,
                        PeriodStartUtc = now,
                        PeriodEndUtc = periodEnd,
                        Reference = "Free Trial / الفترة الأولى عند التفعيل"
                    };

                    lab.SubscriptionPayments ??= new List<LabSubscriptionPayment>();
                    lab.SubscriptionPayments.Add(subscriptionPayment);

                    lab.SubscriptionStartUtc = now;
                    lab.SubscriptionEndUtc = periodEnd;

                    await _repo.SaveChangesAsync();
                }

                // 🌟 إرسال إيميل التفعيل والترحيب بمستخدم المخبر
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var emailSubject = "Dental Lab Platform | تفعيل حساب المخبر";
                    var userName = user.Name ?? "مخبرنا العزيز";

                    var emailBody = $"مرحباً {userName}،\n\n" +
                                    $"تم قبول وتفعيل حساب المخبر الخاص بك بنجاح على منصة Dental Lab Platform.\n\n" +
                                    $"حسابك الآن يعمل ضمن (الوضع المجاني) لمدة شهر كامل، بدءاً من {DateTime.UtcNow:yyyy-MM-dd} وحتى {DateTime.UtcNow.AddMonths(1):yyyy-MM-dd}.\n" +
                                    $"قيمة الاشتراك الشهري المحددة لحسابك بعد انتهاء الفترة المجانية هي: {amount:C}\n\n" +
                                    $"نتمنى لك تجربة موفقة على منصتنا.\n\n" +
                                    $"مع تحيات،\n" +
                                    $"فريق منصة Dental Lab Platform";

                    await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                }
            }

            return null;
        }

        public Task<string?> RejectLabAsync(int id)
            => SetLabStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: true);

        public Task<string?> SuspendLabAsync(int id)
            => SetLabStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: false);

        private async Task<string?> SetDentistStatusAsync(int id, AccountStatus status, bool requirePendingAdminApproval)
        {
            var user = await _repo.GetUserByIdTrackingAsync(id);
            if (user == null) return "User not found.";
            if (user.Role != UserRole.Dentist) return "User is not a dentist.";
            if (requirePendingAdminApproval && user.Status != AccountStatus.PendingAdminApproval)
                return "Dentist is not pending admin approval.";
            if (user.Status == status) return "Dentist already has this status.";

            user.Status = status;
            await _repo.SaveChangesAsync();

            // 🌟 إرسال إيميل التفعيل لطبيب الأسنان
            if (status == AccountStatus.Active && !string.IsNullOrWhiteSpace(user.Email))
            {
                var userName = user.Name ?? "طبيبنا العزيز";
                var emailSubject = "Dental Lab Platform | تفعيل حساب الطبيب";
                var emailBody = $"مرحباً {userName}،\n\nيسعدنا إخبارك بأن حسابك على منصة Dental Lab Platform قد تم مراجعته والموافقة عليه بنجاح.\n\nيمكنك الآن تسجيل الدخول والاستفادة من كافة خدمات المنصة.\n\nمع تحيات،\nفريق منصة Dental Lab Platform";

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
            }

            return null;
        }

        private async Task<string?> SetLabStatusAsync(int id, AccountStatus status, bool requirePendingAdminApproval)
        {
            var user = await _repo.GetUserByIdTrackingAsync(id);
            if (user == null) return "User not found.";
            if (user.Role != UserRole.Lab) return "User is not a lab.";
            if (requirePendingAdminApproval && user.Status != AccountStatus.PendingAdminApproval)
            {
                return "Lab is not pending admin approval.";
            }

            if (user.Status == status) return "Lab already has this status.";

            user.Status = status;
            await _repo.SaveChangesAsync();
            return null;
        }


    }
}