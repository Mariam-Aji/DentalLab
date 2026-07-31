namespace DentalLab.Api.Dtos;

// ─── فواتير الطلبيات ───────────────────────────────────────────────

public class LabPaidOrderInvoiceDto
{
    public int InvoiceId { get; set; }
    public int CaseOrderId { get; set; }
    public string CaseOrderTitle { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;

    /// <summary>
    /// السعر النهائي الذي حدده المخبر على الطلبية
    /// </summary>
    public decimal? FinalPrice { get; set; }

    /// <summary>
    /// إجمالي الفاتورة المحسوب من بنودها
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// تاريخ ووقت الدفع
    /// </summary>
    public DateTime? PaidAt { get; set; }

    public List<LabPaidOrderInvoiceItemDto> Items { get; set; } = new();
}

public class LabPaidOrderInvoiceItemDto
{
    public string CompensationType { get; set; } = string.Empty;
    public string ToothNumbers { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int TeethCount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>
    /// تنبيه يظهر عندما يكون سعر نوع التعويض لم يُدخله المخبر (السعر = صفر)
    /// </summary>
    public string? PriceNote { get; set; }
}

// ─── فواتير الإعلانات ─────────────────────────────────────────────

public class LabPaidAdInvoiceDto
{
    public int AdvertisementId { get; set; }
    public string AdTitle { get; set; } = string.Empty;
    public string AdContent { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>
    /// تاريخ ووقت الدفع
    /// </summary>
    public DateTime? PaidAt { get; set; }
}

// ─── الرد الموحد ──────────────────────────────────────────────────

public class LabPaidInvoicesResponseDto
{
    public List<LabPaidOrderInvoiceDto> OrderInvoices { get; set; } = new();
    public List<LabPaidAdInvoiceDto> AdInvoices { get; set; } = new();
}
