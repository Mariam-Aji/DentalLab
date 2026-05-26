public class CaseOrderInvoiceDto
{
    public int CaseOrderId { get; set; }
    public string Status { get; set; } = string.Empty;

    public decimal EstimatedTotal { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<CaseOrderInvoiceItemDto> Items { get; set; } = new();
}