namespace DentalLab.Api.Dtos
{
    public class GatewayPaymentRequest
    {
        public string NotificationOption { get; set; } = "LNK";
        public decimal InvoiceValue { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DisplayCurrencyIso { get; set; } = "KWD";
        public string CallBackUrl { get; set; } = string.Empty;
        public string ErrorUrl { get; set; } = string.Empty;
        public string CustomerReference { get; set; } = string.Empty;
    }
}
