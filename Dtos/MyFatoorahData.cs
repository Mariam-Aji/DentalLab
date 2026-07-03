namespace DentalLab.Api.Dtos
{
    public class MyFatoorahData
    {
        public int InvoiceId { get; set; }

        public string InvoiceURL { get; set; } = string.Empty;

        public string InvoiceStatus { get; set; } = string.Empty;

        public string PaymentId { get; set; } = string.Empty;
    }
}