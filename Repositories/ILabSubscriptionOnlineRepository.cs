using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabSubscriptionOnlineRepository
{
    /// <summary>جلب المخبر مع مالكه وكل سجلات دفعاته عبر userId</summary>
    Task<Lab?> GetLabWithPaymentsByUserIdAsync(int userId);

    /// <summary>جلب المخبر مع مالكه وكل سجلات دفعاته عبر labId</summary>
    Task<Lab?> GetLabWithPaymentsByLabIdAsync(int labId);

    /// <summary>جلب سجل الفترة المجانية للمخبر (Reference يحتوي "Free Trial")</summary>
    Task<LabSubscriptionPayment?> GetFreeTrialPaymentAsync(int labId);

    /// <summary>جلب آخر سجل دفع للمخبر كـ fallback للسعر</summary>
    Task<LabSubscriptionPayment?> GetLatestPaymentByLabIdAsync(int labId);

    /// <summary>إضافة سجل دفع اشتراك جديد</summary>
    Task AddSubscriptionPaymentAsync(LabSubscriptionPayment payment);

    /// <summary>تحديث بيانات المخبر ومستخدمه</summary>
    Task UpdateLabAndUserAsync(Lab lab, User user);
}
