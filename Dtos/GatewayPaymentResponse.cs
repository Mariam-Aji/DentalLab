namespace DentalLab.Api.Dtos
{
    public class GatewayPaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public GatewayPaymentData? Data { get; set; }
    }
}
