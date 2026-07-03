namespace DentalLab.Api.Dtos;

/// <summary>
/// سطر واحد في عرض السعر (نوع تعويض واحد)
/// </summary>
public class OrderQuoteLineDto
{
    public string CompensationType { get; set; } = "";
    public string CompensationTypeAr { get; set; } = "";
    public int Quantity { get; set; }           // عدد الأسنان
    public List<int> ToothNumbers { get; set; } = new();
    public decimal UnitPrice { get; set; }      // سعر السن الواحد
    public decimal LineTotal { get; set; }      // UnitPrice × Quantity
    public bool PriceFound { get; set; }        // هل وُجد سعر مسجل للمخبر
}

/// <summary>
/// عرض السعر الكامل لطلبية — يُعرض للمخبر قبل تأكيد السعر النهائي
/// </summary>
public class OrderQuoteDto
{
    public int OrderId { get; set; }
    public string OrderTitle { get; set; } = "";
    public string DentistName { get; set; } = "";
    public DateTime? DeliveryDate { get; set; }
    public bool IsUrgent { get; set; }

    public List<OrderQuoteLineDto> Lines { get; set; } = new();

    /// <summary>
    /// المجموع التقديري (بناءً على أسعار المخبر المسجلة)
    /// </summary>
    public decimal EstimatedTotal { get; set; }

    /// <summary>
    /// السعر النهائي الذي أدخله المخبر (null إذا لم يُحدَّد بعد)
    /// </summary>
    public decimal? FinalPrice { get; set; }

    public bool IsPaid { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// المخبر يرسل السعر النهائي
/// </summary>
public class SetFinalPriceDto
{
    public decimal FinalPrice { get; set; }
}
