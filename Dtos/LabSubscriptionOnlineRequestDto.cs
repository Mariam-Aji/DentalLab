namespace DentalLab.Api.Dtos;

/// <summary>
/// طلب المخبر لبدء الدفع الإلكتروني لاشتراكه عبر MyFatoorah
/// المخبر يختار عدد الشهور فقط — السعر يُحسب تلقائياً
/// </summary>
public class LabSubscriptionOnlineRequestDto
{
    /// <summary>
    /// عدد الشهور المراد الدفع عنها (1 إلى 12)
    /// </summary>
    public int Months { get; set; }

    /// <summary>
    /// رمز العملة — اختياري، الافتراضي USD
    /// </summary>
    public string Currency { get; set; } = "USD";
}
