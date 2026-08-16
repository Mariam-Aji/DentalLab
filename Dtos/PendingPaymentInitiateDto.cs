namespace DentalLab.Api.Dtos;

/// <summary>
/// طلب بدء الدفع لمخبر حسابه PendingPayment (بدون توكن)
/// </summary>
public class PendingPaymentInitiateDto
{
    /// <summary>معرّف المخبر (Lab.Id)</summary>
    public int LabId { get; set; }

    /// <summary>معرّف المستخدم (User.Id)</summary>
    public int UserId { get; set; }

    /// <summary>عدد الشهور المراد الدفع عنها (1 إلى 12)</summary>
    public int Months { get; set; }

    /// <summary>رمز العملة — اختياري، الافتراضي USD</summary>
    public string Currency { get; set; } = "USD";
}
