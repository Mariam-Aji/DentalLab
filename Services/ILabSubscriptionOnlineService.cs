using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabSubscriptionOnlineService
{
    /// <summary>
    /// يُرجع حالة اشتراك المخبر الحالية (نشط / منتهي / فترة مجانية + تاريخ الانتهاء)
    /// </summary>
    Task<(LabSubscriptionStatusDto? Result, string? Error)> GetMyStatusAsync(int userId);

    /// <summary>
    /// يُنشئ رابط دفع لمخبر حسابه PendingPayment (بدون توكن)
    /// يتحقق أن الحساب فعلاً PendingPayment قبل إنشاء الرابط
    /// </summary>
    Task<(LabSubscriptionOnlineResponseDto? Result, string? Error)> InitiatePendingPaymentAsync(PendingPaymentInitiateDto dto);

    /// <summary>
    /// يُرجع جدول الأسعار لمخبر حسابه PendingPayment (بدون توكن)
    /// </summary>
    Task<(LabSubscriptionPriceInfoDto? Result, string? Error)> GetPendingPriceInfoAsync(int labId, int userId);

    /// <summary>
    /// يُرجع السعر الشهري + جدول الأسعار — يستدعيه المخبر قبل الدفع
    /// </summary>
    Task<(LabSubscriptionPriceInfoDto? Result, string? Error)> GetPriceInfoAsync(int userId);

    /// <summary>
    /// يُنشئ رابط دفع MyFatoorah بناءً على عدد الشهور المختارة
    /// يعمل سواء كان اشتراكاً أولياً (بعد الفترة المجانية) أو تجديداً
    /// </summary>
    Task<(LabSubscriptionOnlineResponseDto? Result, string? Error)> InitiatePaymentAsync(int userId, LabSubscriptionOnlineRequestDto dto);

    /// <summary>
    /// يُكمل تجديد الاشتراك بعد نجاح الدفع — يُستدعى من MyFatoorahService.VerifyPaymentAsync
    /// </summary>
    Task<(bool Success, string Message)> FinalizeSubscriptionAsync(int labId, decimal paidAmount, int months, string invoiceId);

    /// <summary>
    /// يتحقق من الدفع عبر MyFatoorah ثم يُجدد الاشتراك إذا نجح
    /// يُستدعى من Controller مباشرة بعد عودة المخبر من صفحة الدفع
    /// </summary>
    Task<(bool Success, string Message)> VerifyAndFinalizeAsync(int userId, string paymentId);
    /// <summary>
    /// يتحقق من الدفع عبر MyFatoorah ثم يُجدد الاشتراك إذا نجح — عبر labId (للـ PendingPayment بدون توكن)
    /// </summary>
    Task<(bool Success, string Message)> VerifyAndFinalizeByLabIdAsync(int labId, string paymentId);

    /// <summary>
    /// يتحقق من الدفع ويُجدد الاشتراك تلقائياً — يُستدعى من callback MyFatoorah
    /// يستخرج labId من CustomerReference في الفاتورة
    /// </summary>
    Task<(bool Success, string Message)> VerifyAndFinalizeByPaymentIdAsync(string paymentId);
}
