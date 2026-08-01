using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DentalLab.Api.Models;
using DentalLab.Api.Services;

public class MyFatoorahService : IMyFatoorahService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<MyFatoorahService> _logger;

    public MyFatoorahService(
        IPaymentRepository paymentRepo,
        HttpClient httpClient,
        IConfiguration config,
        IEmailService emailService,
        ILogger<MyFatoorahService> logger)
    {
        _paymentRepo = paymentRepo;
        _config = config;
        _emailService = emailService;
        _logger = logger;

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
        var order = await _paymentRepo.GetOrderWithUserAndLabAsync(orderId);

        if (order == null)
            return (false, null, "الطلبية غير موجودة.");

        if (order.CreatedById != doctorId)
            return (false, null, "غير مصرح لك بالدفع لهذه الطلبية.");

        if (order.IsPaid)
            return (false, null, "هذه الطلبية مدفوعة بالكامل بالفعل.");

        if (order.Status != CaseStatus.Ready)
            return (false, null, "لا يمكن إجراء الدفع، الطلبية ليست بحالة جاهزة (Ready).");

        if (order.AssignedLab == null)
            return (false, null, "هذه الطلبية غير مسندة لأي مخبر.");

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
            finalCurrency = "KWD";
        }

        var payload = new Dictionary<string, object>
        {
            { "NotificationOption", "LNK" },
            { "InvoiceValue", totalAmount },
            { "DisplayCurrencyIso", finalCurrency },
            { "CustomerName", order.CreatedBy?.Name ?? "Dentist Guest" },
            { "CustomerEmail", order.CreatedBy?.Email ?? "test@example.com" },
            { "CustomerMobile", order.CreatedBy?.Phone ?? "00000000" },
            { "CustomerReference", order.Id.ToString() },
            { "CallBackUrl", callbackUrl },
            { "ErrorUrl", errorUrl },
            { "Language", "ar" }
        };

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

        return await SendPaymentRequestToGateway(payload, apiKey, baseUrl);
    }

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

        var payload = new Dictionary<string, object>
        {
            { "NotificationOption", "LNK" },
            { "InvoiceValue", adPrice },
            { "DisplayCurrencyIso", finalCurrency },
            { "CustomerName", ad.User?.Name ?? "Ad Client" },
            { "CustomerEmail", ad.User?.Email ?? "test@example.com" },
            { "CustomerMobile", ad.User?.Phone ?? "00000000" },
            { "CustomerReference", $"AD_{ad.Id}" },
            { "CallBackUrl", callbackUrl },
            { "ErrorUrl", errorUrl },
            { "Language", "ar" }
        };

        return await SendPaymentRequestToGateway(payload, apiKey, baseUrl);
    }

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

    // --- التحقق الموحد مع التمييز بين الإعلانات والطلبيات وإرسال الإيميل ---
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

            // استخراج رقم الفاتورة الخاص ببوابة الدفع
            string invoiceId = data.TryGetProperty("InvoiceId", out var invProp)
                ? invProp.GetInt32().ToString()
                : paymentId;

            if (invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            {
                // 1. حالة الدفع مخصصة لإعلان
                if (referenceStr.StartsWith("AD_") && int.TryParse(referenceStr.Substring(3), out int adId))
                {
                    var updatedAd = await _paymentRepo.UpdateAdPaymentStatusAsync(adId, paidAmount, true);

                    if (updatedAd?.User != null && !string.IsNullOrEmpty(updatedAd.User.Email))
                    {
                        decimal finalPaidValue = paidAmount > 0 ? paidAmount : (updatedAd.Price ?? 0);

                        // الإعلان مدفوع لصالح إدارة / مالك المنصة
                        string payeeName = "إدارة منصة DentalLab";

                        await SendReceiptEmailAsync(
                            toEmail: updatedAd.User.Email,
                            userName: updatedAd.User.Name,
                            itemTitle: $"حجز إعلان: {updatedAd.Title}",
                            amount: finalPaidValue,
                            payeeName: payeeName,
                            invoiceId: invoiceId
                        );
                    }

                    return (true, "Paid", null);
                }
                // 2. حالة الدفع مخصصة لطلبية مخبر
                else if (int.TryParse(referenceStr, out int orderId))
                {
                    var updatedOrder = await _paymentRepo.UpdateOrderPaymentStatusAsync(orderId, paidAmount, true);

                    if (updatedOrder?.CreatedBy != null && !string.IsNullOrEmpty(updatedOrder.CreatedBy.Email))
                    {
                        decimal finalPaidValue = paidAmount > 0 ? paidAmount : (updatedOrder.FinalPrice ?? updatedOrder.EstimatedPrice ?? 0);

                        // استخراج اسم المخبر المباشر من حقل NamePlace لمالك المخبر (Owner)
                        var labOwner = updatedOrder.AssignedLab?.Owner;
                        string labName = !string.IsNullOrWhiteSpace(labOwner?.NamePlace)
                            ? labOwner.NamePlace
                            : (!string.IsNullOrWhiteSpace(labOwner?.Name) ? labOwner.Name : "المخبر المستلم");

                        await SendReceiptEmailAsync(
                            toEmail: updatedOrder.CreatedBy.Email,
                            userName: updatedOrder.CreatedBy.Name,
                            itemTitle: $"طلبية حالة: {updatedOrder.Title}",
                            amount: finalPaidValue,
                            payeeName: labName,
                            invoiceId: invoiceId
                        );
                    }

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
    // دالة مساعدة لإرسال إيميل الفاتورة بالتنسيق الجديد
    private async Task SendReceiptEmailAsync(
     string toEmail,
     string userName,
     string itemTitle,
     decimal amount,
     string payeeName,
     string invoiceId)
    {
        try
        {
            string subject = $"إيصال تأكيد الدفع - فاتورة رقم #{invoiceId}";
            string body = $@"
            <div dir='rtl' style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; text-align: center; border-bottom: 2px solid #27ae60; padding-bottom: 10px;'>إيصال استلام الدفعة</h2>
                <p>مرحباً <strong>{userName}</strong>،</p>
                <p>تمت عملية الدفع الخاصة بك بنجاح، وفيما يلي تفاصيل الفاتورة:</p>
                
                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                    <tr style='background-color: #f8f9fa;'>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>رقم الفاتورة:</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold; color: #2c3e50;'>#{invoiceId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>تفاصيل الخدمة:</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{itemTitle}</td>
                    </tr>
                    <tr style='background-color: #f8f9fa;'>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>تم الدفع لصالح:</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold; color: #2980b9;'>{payeeName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>المبلغ المدفوع:</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd; color: #27ae60; font-weight: bold;'>{amount:N2} USD</td>
                    </tr>
                    <tr style='background-color: #f8f9fa;'>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>تاريخ ووقت الدفع:</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</td>
                    </tr>
                </table>

                <p style='margin-top: 20px; text-align: center; color: #7f8c8d; font-size: 12px;'>هذه الرسالة تم إنشاؤها آلياً من منصة DentalLab.</p>
            </div>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء إرسال إيصال الدفع عبر البريد الإلكتروني.");
        }
    }

    // دالة مساعدة لإرسال إيميل الفاتورة
    private async Task SendReceiptEmailAsync(string toEmail, string userName, string itemTitle, decimal amount)
    {
        try
        {
            string subject = "تأكيد إتمام عملية الدفع بنجاح";
            string body = $@"
                <div dir='rtl' style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #2c3e50; text-align: center;'>إيصال استلام الدفعة</h2>
                    <p>مرحباً <strong>{userName}</strong>،</p>
                    <p>نشكرك على استخدام منصتنا. تمت عملية الدفع الخاصة بك بنجاح، وفيما يلي تفاصيل الفاتورة:</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                        <tr style='background-color: #f8f9fa;'>
                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>الخدمة / المادة:</strong></td>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{itemTitle}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>المبلغ المدفوع:</strong></td>
                            <td style='padding: 10px; border: 1px solid #ddd; color: #27ae60; font-weight: bold;'>{amount:N2} USD</td>
                        </tr>
                        <tr style='background-color: #f8f9fa;'>
                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>تاريخ ووقت الدفع:</strong></td>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</td>
                        </tr>
                    </table>

                    <p style='margin-top: 20px; text-align: center; color: #7f8c8d; font-size: 12px;'>هذه الرسالة تم إنشاؤها آلياً، يرجى عدم الرد عليها مباشرة.</p>
                </div>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
        catch
        {
            // تجنب إيقاف عملية الدفع في حال حدوث خلل مؤقت في خادم البريد
        }
    }
}