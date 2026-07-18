using System.Threading.Tasks;

public interface IMyFatoorahService
{
    Task<(bool Success, string? PaymentUrl, string? Error)> ProcessOrderPaymentAsync(int orderId, int doctorId, string currency = "USD");
    Task<(bool Success, string Status, string? Error)> VerifyPaymentAsync(string paymentId);
}