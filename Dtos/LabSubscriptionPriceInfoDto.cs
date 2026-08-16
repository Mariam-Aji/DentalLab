namespace DentalLab.Api.Dtos;

/// <summary>
/// معلومات سعر الاشتراك — يُرجعها endpoint منفصل ليعرض المخبر السعر قبل الدفع
/// </summary>
public class LabSubscriptionPriceInfoDto
{
    /// <summary>السعر الشهري المحدد من الأدمن</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>هل الاشتراك الحالي نشط؟</summary>
    public bool IsActive { get; set; }

    /// <summary>تاريخ انتهاء الاشتراك الحالي (null إذا لم يكن هناك اشتراك)</summary>
    public DateTime? CurrentSubscriptionEndUtc { get; set; }

    /// <summary>الأيام المتبقية على انتهاء الاشتراك الحالي</summary>
    public int RemainingDays { get; set; }

    /// <summary>جدول الأسعار من 1 إلى 12 شهراً</summary>
    public List<SubscriptionPriceTierDto> PriceTiers { get; set; } = new();
}

public class SubscriptionPriceTierDto
{
    public int Months { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime NewPeriodEndUtc { get; set; }
}
