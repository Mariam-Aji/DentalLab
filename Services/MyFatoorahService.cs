using System;
using System.Collections.Generic;
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
        return rawApiKey.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
    }

    private string GetCleanBaseUrl()
    {
        string baseUrl = _config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com";
        return baseUrl.Trim().TrimEnd('/');
    }

    public async Task<(bool Success, string? PaymentUrl, string? Error)> ProcessOrderPaymentAsync(int orderId, int doctorId, string currency = "USD")
    {
        // 1. جلب الطلبية محملة ببيانات الطبيب والمخبر
        var order = await _paymentRepo.GetOrderWithUserAndLabAsync(orderId);

        if (order == null)
            return (false, null, "الطلبية غير موجودة.");

        if (order.CreatedById != doctorId)
            return (false, null, "غير مصرح لك بالدفع لهذه الطلبية.");

        if (order.IsPaid)
            return (false, null, "هذه الطلبية مدفوعة بالكامل بالفعل.");

        // الشرط المطلوب: الدفع متاح فقط عندما تكون حالة الطلبية Ready
        if (order.Status != CaseStatus.Ready)
            return (false, null, "لا يمكن إجراء الدفع، الطلبية ليست بحالة جاهزة (Ready).");

        if (order.AssignedLab == null)
            return (false, null, "هذه الطلبية غير مسندة لأي مخبر.");

        // قراءة السعر الإجمالي
        decimal totalAmount = order.FinalPrice ?? order.EstimatedPrice ?? 0;
        if (totalAmount <= 0)
            return (false, null, "لا يمكن الدفع لطلب قيمته صفر أو غير محدد.");

        string apiKey = GetCleanApiKey();
        string baseUrl = GetCleanBaseUrl();
        string callbackUrl = (_config["MyFatoorah:CallbackUrl"] ?? "").Trim();
        string errorUrl = (_config["MyFatoorah:ErrorUrl"] ?? "").Trim();

        string finalCurrency = currency.ToUpper();
        if (baseUrl.Contains("apitest") && (finalCurrency == "USD" || string.IsNullOrEmpty(finalCurrency)))
        {
            finalCurrency = "KWD"; // بيئة الاختبار في MyFatoorah تفضل KWD
        }

        // 2. بناء جسم الطلب الديناميكي
        var payload = new Dictionary<string, object>
        {
            { "NotificationOption", "LNK" },
            { "InvoiceValue", totalAmount },
            { "DisplayCurrencyIso", finalCurrency },
            // 👈 المرسل/الدافع (بيانات الطبيب)
            { "CustomerName", order.CreatedBy?.Name ?? "Dentist Guest" },
            { "CustomerEmail", order.CreatedBy?.Email ?? "test@example.com" },
            { "CustomerMobile", order.CreatedBy?.Phone ?? "00000000" },
            { "CustomerReference", order.Id.ToString() },
            { "CallBackUrl", callbackUrl },
            { "ErrorUrl", errorUrl },
            { "Language", "ar" }
        };

        // 👈 المستقبل/المستفيد (إذا كان المخبر يملك SupplierCode في MyFatoorah)
        if (order.AssignedLab.MyFatoorahSupplierCode.HasValue)
        {
            payload["Suppliers"] = new[]
            {
                new
                {
                    SupplierCode = order.AssignedLab.MyFatoorahSupplierCode.Value,
                    ProposedShare = totalAmount,
                    InvoiceShare = totalAmount
                }
            };
        }

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

    //public async Task<(bool Success, string Status, string? Error)> VerifyPaymentAsync(string paymentId)
    //{
    //    var payload = new { KeyType = "PaymentId", Key = paymentId };

    //    string apiKey = GetCleanApiKey();
    //    string baseUrl = GetCleanBaseUrl();

    //    try
    //    {
    //        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/GetPaymentStatus");
    //        request.Headers.Clear();
    //        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    //        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
    //        request.Content = JsonContent.Create(payload);

    //        var response = await _httpClient.SendAsync(request);
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            var errorContent = await response.Content.ReadAsStringAsync();
    //            return (false, "Failed", $"فشل التحقق من حالة الفاتورة. التفاصيل: {errorContent}");
    //        }

    //        var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
    //        if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
    //            return (false, "Failed", "البوابة رفضت عملية التحقق.");

    //        var data = jsonResult.GetProperty("Data");
    //        string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
    //        string orderIdStr = data.GetProperty("CustomerReference").GetString()!;
    //        decimal paidAmount = data.GetProperty("InvoiceValue").GetDecimal();

    //        // عند نجاح الدفع يُعدل حقل IsPaid إلى true
    //        if (invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase) && int.TryParse(orderIdStr, out int orderId))
    //        {
    //            await _paymentRepo.UpdateOrderPaymentStatusAsync(orderId, paidAmount, true);
    //            return (true, "Paid", null);
    //        }

    //        return (false, invoiceStatus, "عملية الدفع لم تكتمل بعد.");
    //    }
    //    catch (Exception ex)
    //    {
    //        return (false, "Error", $"حدث خطأ أثناء التحقق: {ex.Message}");
    //    }
    //}
    public async Task<(bool Success, string? PaymentUrl, string? Error)> ProcessAdPaymentAsync(int adId, int userId, string currency = "USD")
    {
        var ad = await _paymentRepo.GetAdvertisementWithUserAsync(adId);

        if (ad == null)
            return (false, null, "الإعلان غير موجود.");

        if (ad.UserId != userId)
            return (false, null, "غير مصرح لك بالدفع لهذا الإعلان.");

        if (ad.IsPaid)
            return (false, null, "هذا الإعلان مدفوع بالفعل.");

        decimal adPrice = ad.Price ?? 0;
        if (adPrice <= 0)
            return (false, null, "قيمة الإعلان غير صالحة أو مجانية.");

        string apiKey = GetCleanApiKey();
        string baseUrl = GetCleanBaseUrl();
        string callbackUrl = (_config["MyFatoorah:CallbackUrl"] ?? "").Trim();
        string errorUrl = (_config["MyFatoorah:ErrorUrl"] ?? "").Trim();

        string finalCurrency = currency.ToUpper();
        if (baseUrl.Contains("apitest") && (finalCurrency == "USD" || string.IsNullOrEmpty(finalCurrency)))
        {
            finalCurrency = "KWD";
        }

        // بناء الطلب: موجه بالكامل للمنصة (بدون حقل Suppliers)
        var payload = new Dictionary<string, object>
        {
            { "NotificationOption", "LNK" },
            { "InvoiceValue", adPrice },
            { "DisplayCurrencyIso", finalCurrency },
            { "CustomerName", ad.User?.Name ?? "Ad Client" },
            { "CustomerEmail", ad.User?.Email ?? "test@example.com" },
            { "CustomerMobile", ad.User?.Phone ?? "00000000" },
            { "CustomerReference", $"AD_{ad.Id}" }, // 👈 استخدام بادئة AD_ لتمييزه عن الطلبيات عند العودة
            { "CallBackUrl", callbackUrl },
            { "ErrorUrl", errorUrl },
            { "Language", "ar" }
        };

        return await SendPaymentRequestToGateway(payload, apiKey, baseUrl);
    }

    // دالة مساعدة لإرسال الطلب لـ MyFatoorah تلافياً لتكرار الكود
    private async Task<(bool Success, string? PaymentUrl, string? Error)> SendPaymentRequestToGateway(Dictionary<string, object> payload, string apiKey, string baseUrl)
    {
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
            return (false, null, $"فشل الاتصال: {ex.Message}");
        }
    }

    // --- التحقق الموحد مع التمييز بين الإعلانات والطلبيات ---
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
                return (false, "Failed", $"فشل التحقق من حالة الفاتورة. التفاصيل: {errorContent}");
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
                return (false, "Failed", "البوابة رفضت عملية التحقق.");

            var data = jsonResult.GetProperty("Data");
            string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
            string referenceStr = data.GetProperty("CustomerReference").GetString()!;
            decimal paidAmount = data.GetProperty("InvoiceValue").GetDecimal();

            if (invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            {
                // 👈 1. إذا كان المرجع يبدأ بـ AD_ فهو دفع خاص بإعلان
                if (referenceStr.StartsWith("AD_") && int.TryParse(referenceStr.Substring(3), out int adId))
                {
                    await _paymentRepo.UpdateAdPaymentStatusAsync(adId, paidAmount, true);
                    return (true, "Paid", null);
                }
                // 👈 2. إذا كان رقماً بحتاً فهو دفع خاص بطلبية مخبر قديمة أو حالية
                else if (int.TryParse(referenceStr, out int orderId))
                {
                    await _paymentRepo.UpdateOrderPaymentStatusAsync(orderId, paidAmount, true);
                    return (true, "Paid", null);
                }
            }

            return (false, invoiceStatus, "عملية الدفع لم تكتمل بعد.");
        }
        catch (Exception ex)
        {
            return (false, "Error", $"حدث خطأ أثناء التحقق: {ex.Message}");
        }
    }
}