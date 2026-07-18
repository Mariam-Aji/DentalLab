using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DentalLab.Api.Models;

public class MyFatoorahService : IMyFatoorahService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public MyFatoorahService(IPaymentRepository paymentRepo, HttpClient httpClient, IConfiguration config)
    {
        _paymentRepo = paymentRepo;
        _config = config;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler);
    }

    
    private string GetCleanApiKey()
    {
        string rawApiKey = _config["MyFatoorah:ApiKey"] ?? "";
        return rawApiKey.Trim()
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace(" ", "");
    }

    
    private string GetCleanBaseUrl()
    {
        string baseUrl = _config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com";
        return baseUrl.Trim().TrimEnd('/');
    }

    public async Task<(bool Success, string? PaymentUrl, string? Error)> ProcessOrderPaymentAsync(int orderId, int doctorId, string currency = "KWD")
    {
        var order = await _paymentRepo.GetOrderWithUserAsync(orderId);

        if (order == null)
            return (false, null, "الطلبية غير موجودة.");

        if (order.CreatedById != doctorId)
            return (false, null, "غير مصرح لك بالدفع لهذه الطلبية.");

        if (order.IsPaid)
            return (false, null, "هذه الطلبية مدفوعة بالكامل بالفعل.");

        decimal totalAmount = order.FinalPrice ?? order.EstimatedPrice ?? 0;
        if (totalAmount <= 0)
            return (false, null, "لا يمكن الدفع لطلب قيمته صفر.");

        double percentage = (order.CreatedBy?.Status == AccountStatus.Active) ? 0.25 : 0.75;
        decimal amountToPay = Math.Round(totalAmount * (decimal)percentage, 2);

        string apiKey = GetCleanApiKey();
        string baseUrl = GetCleanBaseUrl();
        string callbackUrl = (_config["MyFatoorah:CallbackUrl"] ?? "").Trim();
        string errorUrl = (_config["MyFatoorah:ErrorUrl"] ?? "").Trim();

        string finalCurrency = currency.ToUpper();
        if (baseUrl.Contains("apitest") && (finalCurrency == "USD" || string.IsNullOrEmpty(finalCurrency)))
        {
            finalCurrency = "KWD";
        }

        var payload = new
        {
            NotificationOption = "LNK",
            InvoiceValue = amountToPay,
            DisplayCurrencyIso = finalCurrency,
            CustomerName = order.CreatedBy?.Name ?? "Dentist Guest",
            CustomerEmail = order.CreatedBy?.Email ?? "test@example.com",
            CustomerMobile = order.CreatedBy?.Phone ?? "00000000",
            CustomerReference = order.Id.ToString(),
            UserDefinedField = percentage == 0.25 ? "initial" : "remaining",
            CallBackUrl = callbackUrl,
            ErrorUrl = errorUrl,
            Language = "ar"
        };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/SendPayment");

            request.Headers.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, null, $"رفض السيرفر الخارجي الطلب برمز: {response.StatusCode}. التفاصيل: {errorContent}");
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (jsonResult.TryGetProperty("IsSuccess", out var isSuccessProp) && isSuccessProp.GetBoolean())
            {
                var dataObject = jsonResult.GetProperty("Data");
                string paymentUrl = dataObject.GetProperty("InvoiceURL").GetString()!;
                return (true, paymentUrl, null);
            }

            string errMsg = jsonResult.TryGetProperty("Message", out var msgProp) ? msgProp.GetString()! : "استجابة غير صالحة من البوابة.";
            return (false, null, errMsg);
        }
        catch (Exception ex)
        {
            return (false, null, $"فشل الاتصال: {ex.Message} -> {ex.InnerException?.Message}");
        }
    }

    public async Task<(bool Success, string Status, string? Error)> VerifyPaymentAsync(string paymentId)
    {
        var payload = new { KeyType = "PaymentId", Key = paymentId };

        string apiKey = GetCleanApiKey();
        string baseUrl = GetCleanBaseUrl();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/GetPaymentStatus");

            request.Headers.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, "Failed", $"فشل التحقق من حالة الفاتورة. الرمز: {response.StatusCode}. التفاصيل: {errorContent}");
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
                return (false, "Failed", "البوابة رفضت عملية التحقق.");

            var data = jsonResult.GetProperty("Data");
            string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
            string orderIdStr = data.GetProperty("CustomerReference").GetString()!;

            if (invoiceStatus.ToUpper() == "PAID" && int.TryParse(orderIdStr, out int orderId))
            {
                await _paymentRepo.UpdateOrderPaymentStatusAsync(orderId, 0, true);
                return (true, "Paid", null);
            }

            return (false, invoiceStatus, "عملية الدفع لم تكتمل كلياً بعد.");
        }
        catch (Exception ex)
        {
            return (false, "Error", $"حدث خطأ أثناء التحقق: {ex.Message}");
        }
    }
}