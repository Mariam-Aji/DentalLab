namespace DentalLab.Api.Dtos;

/// <summary>
/// حالة اشتراك المخبر — يُرجعها endpoint /api/lab-subscription-online/my-status
/// </summary>
public class LabSubscriptionStatusDto
{
    /// <summary>هل الاشتراك نشط حالياً؟</summary>
    public bool IsActive { get; set; }

    /// <summary>هل هو ضمن الفترة المجانية؟</summary>
    public bool IsFreeTrial { get; set; }

    /// <summary>تاريخ بداية الاشتراك الحالي</summary>
    public DateTime? SubscriptionStartUtc { get; set; }

    /// <summary>تاريخ انتهاء الاشتراك الحالي</summary>
    public DateTime? SubscriptionEndUtc { get; set; }

    /// <summary>الأيام المتبقية (سالبة إذا انتهى)</summary>
    public int RemainingDays { get; set; }

    /// <summary>السعر الشهري المحدد من الأدمن</summary>
    public decimal? MonthlyPrice { get; set; }
}
