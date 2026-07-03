public class GatewayPaymentData
{
    public int InvoiceId { get; set; }
    public string PaymentURL { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty;
}