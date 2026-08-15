namespace DentalLab.Api.Dtos;

/// <summary>
/// استجابة بدء الدفع الإلكتروني — يتضمن السعر ورابط فاتورة MyFatoorah
/// </summary>
public class LabSubscriptionOnlineResponseDto
{
    /// <summary>رابط الدفع على MyFatoorah</summary>
    public string PaymentUrl { get; set; } = null!;

    /// <summary>عدد الشهور المختارة</summary>
    public int Months { get; set; }

    /// <summary>السعر الشهري المحدد من الأدمن</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>المبلغ الإجمالي = السعر الشهري × عدد الشهور</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>تاريخ بداية فترة الاشتراك الجديدة</summary>
    public DateTime PeriodStartUtc { get; set; }

    /// <summary>تاريخ نهاية فترة الاشتراك الجديدة</summary>
    public DateTime PeriodEndUtc { get; set; }

    public string Message { get; set; } = string.Empty;
}
