namespace DentalLab.Api.Dtos
{
    public class MyFatoorahPaymentRequest
    {
        public int PaymentMethodId { get; set; } = 0;

        public decimal InvoiceValue { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string DisplayCurrencyIso { get; set; } = "KWD";

        public string CallBackUrl { get; set; } = string.Empty;

        public string ErrorUrl { get; set; } = string.Empty;

        public string Language { get; set; } = "ar";

        public string NotificationOption { get; set; } = "LNK";
    }
}