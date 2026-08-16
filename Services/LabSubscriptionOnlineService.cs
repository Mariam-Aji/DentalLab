using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DentalLab.Api.Services;

public class LabSubscriptionOnlineService : ILabSubscriptionOnlineService
{
    private readonly ILabSubscriptionOnlineRepository _repo;
    private readonly IMyFatoorahService _myFatoorah;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public LabSubscriptionOnlineService(
        ILabSubscriptionOnlineRepository repo,
        IMyFatoorahService myFatoorah,
        IEmailService emailService,
        IConfiguration config)
    {
        _repo = repo;
        _myFatoorah = myFatoorah;
        _emailService = emailService;
        _config = config;
    }

    // ---------------------------------------------------------------
    // GET: حالة الاشتراك الحالية للمخبر
    // ---------------------------------------------------------------
    public async Task<(LabSubscriptionStatusDto? Result, string? Error)> GetMyStatusAsync(int userId)
    {
        var lab = await _repo.GetLabWithPaymentsByUserIdAsync(userId);
        if (lab == null)
            return (null, "لم يتم العثور على ملف المخبر.");

        var now = DateTime.UtcNow;
        var isActive = lab.SubscriptionEndUtc.HasValue && lab.SubscriptionEndUtc.Value > now;
        var remaining = lab.SubscriptionEndUtc.HasValue
            ? (int)(lab.SubscriptionEndUtc.Value - now).TotalDays
            : 0;

        // التحقق من الفترة المجانية: هل آخر سجل دفع فعّال هو Free Trial؟
        var freeTrial = await _repo.GetFreeTrialPaymentAsync(lab.Id);
        var latestPayment = await _repo.GetLatestPaymentByLabIdAsync(lab.Id);

        bool isFreeTrial = false;
        if (isActive && freeTrial != null && latestPayment != null)
        {
            // المخبر في الفترة المجانية إذا كان آخر سجل دفع هو نفسه سجل الـ Free Trial
            // أي لم يجرِ أي دفع حقيقي بعده
            isFreeTrial = latestPayment.Id == freeTrial.Id;
        }

        // جلب السعر الشهري
        var (monthlyPrice, _) = await ResolveMonthlyPriceAsync(lab.Id);

        return (new LabSubscriptionStatusDto
        {
            IsActive = isActive,
            IsFreeTrial = isFreeTrial,
            SubscriptionStartUtc = lab.SubscriptionStartUtc,
            SubscriptionEndUtc = lab.SubscriptionEndUtc,
            RemainingDays = remaining,
            MonthlyPrice = monthlyPrice
        }, null);
    }

    // ---------------------------------------------------------------
    // InitiatePendingPaymentAsync
    // للمخبر الذي حسابه PendingPayment — بدون توكن
    // يتحقق من labId + userId + حالة الحساب قبل إنشاء رابط الدفع
    // بعد الدفع: FinalizeSubscriptionAsync تُفعّل الحساب وتبعث إيميل
    // ---------------------------------------------------------------
    public async Task<(LabSubscriptionOnlineResponseDto? Result, string? Error)> InitiatePendingPaymentAsync(PendingPaymentInitiateDto dto)
    {
        if (dto.Months < 1 || dto.Months > 12)
            return (null, "عدد الشهور يجب أن يكون بين 1 و 12.");

        // جلب المخبر والتحقق من تطابق userId
        var lab = await _repo.GetLabWithPaymentsByLabIdAsync(dto.LabId);
        if (lab == null)
            return (null, "المخبر غير موجود.");

        if (lab.UserId != dto.UserId)
            return (null, "معرّف المستخدم لا يتطابق مع هذا المخبر.");

        if (lab.Owner == null)
            return (null, "المستخدم المرتبط بهذا المخبر غير موجود.");

        // التحقق أن الحساب فعلاً PendingPayment
        if (lab.Owner.Status != AccountStatus.PendingPayment)
            return (null, $"حساب المخبر ليس في وضع انتظار الدفع. الحالة الحالية: {lab.Owner.Status}");

        // جلب السعر الشهري
        var (monthlyPrice, priceError) = await ResolveMonthlyPriceAsync(lab.Id);
        if (priceError != null)
            return (null, priceError);

        var now = DateTime.UtcNow;
        var isCurrentlyActive = lab.SubscriptionEndUtc.HasValue && lab.SubscriptionEndUtc.Value > now;

        var periodStart = now;
        var periodEnd = isCurrentlyActive
            ? lab.SubscriptionEndUtc!.Value.AddMonths(dto.Months)
            : now.AddMonths(dto.Months);

        var totalAmount = monthlyPrice!.Value * dto.Months;

        // إنشاء رابط الدفع — Reference = "LSUB_{labId}_{months}"
        // نفس الآلية المستخدمة في InitiatePaymentAsync
        var (success, paymentUrl, error) = await ProcessSubscriptionPaymentAsync(
            lab, totalAmount, dto.Months, dto.Currency);

        if (!success)
            return (null, error);

        return (new LabSubscriptionOnlineResponseDto
        {
            PaymentUrl = paymentUrl!,
            Months = dto.Months,
            MonthlyPrice = monthlyPrice.Value,
            TotalAmount = totalAmount,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Message = $"تم إنشاء رابط الدفع. المبلغ: {totalAmount} مقابل {dto.Months} شهر/شهور. أكمل الدفع لتفعيل حسابك."
        }, null);
    }

    // ---------------------------------------------------------------
    // GetPendingPriceInfoAsync — جدول الأسعار للمخبر PendingPayment بدون توكن
    // يتحقق من labId + userId + أن الحساب PendingPayment
    // ---------------------------------------------------------------
    public async Task<(LabSubscriptionPriceInfoDto? Result, string? Error)> GetPendingPriceInfoAsync(int labId, int userId)
    {
        var lab = await _repo.GetLabWithPaymentsByLabIdAsync(labId);
        if (lab == null)
            return (null, "المخبر غير موجود.");

        if (lab.UserId != userId)
            return (null, "معرّف المستخدم لا يتطابق مع هذا المخبر.");

        if (lab.Owner == null)
            return (null, "المستخدم المرتبط بهذا المخبر غير موجود.");

        if (lab.Owner.Status != AccountStatus.PendingPayment)
            return (null, $"حساب المخبر ليس في وضع انتظار الدفع. الحالة الحالية: {lab.Owner.Status}");

        var (monthlyPrice, priceError) = await ResolveMonthlyPriceAsync(lab.Id);
        if (priceError != null)
            return (null, priceError);

        var now = DateTime.UtcNow;
        // حساب منتهٍ → البداية دائماً من اليوم
        var tiers = Enumerable.Range(1, 12).Select(m => new SubscriptionPriceTierDto
        {
            Months = m,
            TotalAmount = monthlyPrice!.Value * m,
            NewPeriodEndUtc = now.AddMonths(m)
        }).ToList();

        return (new LabSubscriptionPriceInfoDto
        {
            MonthlyPrice = monthlyPrice!.Value,
            IsActive = false,
            CurrentSubscriptionEndUtc = lab.SubscriptionEndUtc,
            RemainingDays = 0,
            PriceTiers = tiers
        }, null);
    }

    // ---------------------------------------------------------------
    // جلب السعر الشهري:
    // 1) سجل Free Trial (Reference يحتوي "Free Trial") — الأدمن حدده عند الموافقة
    // 2) fallback: آخر سجل دفع للمخبر
    // ---------------------------------------------------------------
    private async Task<(decimal? Price, string? Error)> ResolveMonthlyPriceAsync(int labId)    {
        var freeTrial = await _repo.GetFreeTrialPaymentAsync(labId);
        if (freeTrial != null)
            return (freeTrial.Amount, null);

        var latest = await _repo.GetLatestPaymentByLabIdAsync(labId);
        if (latest != null)
            return (latest.Amount, null);

        return (null, "لم يتم تحديد سعر الاشتراك الشهري لهذا المخبر بعد. يرجى التواصل مع الإدارة.");
    }

    // ---------------------------------------------------------------
    // GET: معلومات السعر + جدول الأسعار من 1 إلى 12 شهراً
    // ---------------------------------------------------------------
    public async Task<(LabSubscriptionPriceInfoDto? Result, string? Error)> GetPriceInfoAsync(int userId)
    {
        var lab = await _repo.GetLabWithPaymentsByUserIdAsync(userId);
        if (lab == null)
            return (null, "لم يتم العثور على ملف المخبر.");

        var (monthlyPrice, priceError) = await ResolveMonthlyPriceAsync(lab.Id);
        if (priceError != null)
            return (null, priceError);

        var now = DateTime.UtcNow;
        var isActive = lab.SubscriptionEndUtc.HasValue && lab.SubscriptionEndUtc.Value > now;
        var remaining = isActive ? (int)(lab.SubscriptionEndUtc!.Value - now).TotalDays : 0;

        // نقطة البداية للعرض = اليوم دائماً
        // النهاية = إذا نشط تضاف فوق النهاية الحالية، إذا لا من اليوم
        var baseEnd = isActive ? lab.SubscriptionEndUtc!.Value : now;

        var tiers = Enumerable.Range(1, 12).Select(m => new SubscriptionPriceTierDto
        {
            Months = m,
            TotalAmount = monthlyPrice!.Value * m,
            NewPeriodEndUtc = baseEnd.AddMonths(m)
        }).ToList();

        return (new LabSubscriptionPriceInfoDto
        {
            MonthlyPrice = monthlyPrice!.Value,
            IsActive = isActive,
            CurrentSubscriptionEndUtc = lab.SubscriptionEndUtc,
            RemainingDays = remaining,
            PriceTiers = tiers
        }, null);
    }

    // ---------------------------------------------------------------
    // POST: بدء الدفع الإلكتروني — ينشئ رابط MyFatoorah
    // يعمل للاشتراك الأول (بعد الفترة المجانية) والتجديد
    // CustomerReference = "LSUB_{labId}_{months}" لتمييزه في الـ callback
    // ---------------------------------------------------------------
    public async Task<(LabSubscriptionOnlineResponseDto? Result, string? Error)> InitiatePaymentAsync(int userId, LabSubscriptionOnlineRequestDto dto)
    {
        if (dto.Months < 1 || dto.Months > 12)
            return (null, "عدد الشهور يجب أن يكون بين 1 و 12.");

        var lab = await _repo.GetLabWithPaymentsByUserIdAsync(userId);
        if (lab == null)
            return (null, "لم يتم العثور على ملف المخبر.");

        if (lab.Owner == null)
            return (null, "المستخدم المرتبط بهذا المخبر غير موجود.");

        var (monthlyPrice, priceError) = await ResolveMonthlyPriceAsync(lab.Id);
        if (priceError != null)
            return (null, priceError);

        var now = DateTime.UtcNow;
        var isCurrentlyActive = lab.SubscriptionEndUtc.HasValue && lab.SubscriptionEndUtc.Value > now;

        // البداية دائماً = اليوم
        var periodStart = now;

        // النهاية: إذا نشط نضيف فوق النهاية الحالية، إذا منتهي من اليوم
        var periodEnd = isCurrentlyActive
            ? lab.SubscriptionEndUtc!.Value.AddMonths(dto.Months)
            : now.AddMonths(dto.Months);

        var totalAmount = monthlyPrice!.Value * dto.Months;

        // إنشاء رابط الدفع عبر MyFatoorah
        var (success, paymentUrl, error) = await ProcessSubscriptionPaymentAsync(
            lab, totalAmount, dto.Months, dto.Currency);

        if (!success)
            return (null, error);

        return (new LabSubscriptionOnlineResponseDto
        {
            PaymentUrl = paymentUrl!,
            Months = dto.Months,
            MonthlyPrice = monthlyPrice.Value,
            TotalAmount = totalAmount,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Message = $"تم إنشاء رابط الدفع بنجاح. المبلغ الإجمالي: {totalAmount} مقابل {dto.Months} شهر/شهور."
        }, null);
    }

    // ---------------------------------------------------------------
    // دالة مساعدة: إرسال طلب الدفع لـ MyFatoorah
    // CustomerReference = "LSUB_{labId}_{months}" للتمييز في الـ callback
    // ---------------------------------------------------------------
    private async Task<(bool Success, string? PaymentUrl, string? Error)> ProcessSubscriptionPaymentAsync(
        Lab lab, decimal totalAmount, int months, string currency)
    {
        string apiKey = GetCleanValue(_config["MyFatoorah:ApiKey"] ?? "");
        string baseUrl = GetCleanValue(_config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com").TrimEnd('/');

        // للاشتراك نستخدم callback خاص به إن وُجد، وإلا نرجع للـ callback العام
        string callbackUrl = GetCleanValue(
            _config["MyFatoorah:SubscriptionCallbackUrl"]
            ?? _config["MyFatoorah:CallbackUrl"]
            ?? "");
        string errorUrl = GetCleanValue(_config["MyFatoorah:ErrorUrl"] ?? "");

        string finalCurrency = currency.ToUpper();
        if (baseUrl.Contains("apitest") && (finalCurrency == "USD" || string.IsNullOrEmpty(finalCurrency)))
            finalCurrency = "KWD";

        // المرجع: LSUB_{labId}_{months} — يُستخدم في VerifyPayment للتعرف على نوع الدفع
        string customerReference = $"LSUB_{lab.Id}_{months}";

        var payload = new Dictionary<string, object>
        {
            { "NotificationOption", "LNK" },
            { "InvoiceValue", totalAmount },
            { "DisplayCurrencyIso", finalCurrency },
            { "CustomerName", lab.Owner?.Name ?? "Lab User" },
            { "CustomerEmail", lab.Owner?.Email ?? "noreply@dentallab.com" },
            { "CustomerMobile", lab.Owner?.Phone ?? "00000000" },
            { "CustomerReference", customerReference },
            { "CallBackUrl", callbackUrl },
            { "ErrorUrl", errorUrl },
            { "Language", "ar" }
        };

        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var httpClient = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/SendPayment");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, null, $"رفض السيرفر الطلب: {response.StatusCode}. {errorContent}");
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (jsonResult.TryGetProperty("IsSuccess", out var isSuccessProp) && isSuccessProp.GetBoolean())
            {
                var data = jsonResult.GetProperty("Data");
                string url = data.GetProperty("InvoiceURL").GetString()!;
                return (true, url, null);
            }

            string errMsg = jsonResult.TryGetProperty("Message", out var msgProp)
                ? msgProp.GetString()! : "استجابة غير صالحة من البوابة.";
            return (false, null, errMsg);
        }
        catch (Exception ex)
        {
            return (false, null, $"فشل الاتصال بـ MyFatoorah: {ex.Message}");
        }
    }

    private static string GetCleanValue(string val)
        => val.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

    // ---------------------------------------------------------------
    // VerifyAndFinalizeAsync — يتحقق من الدفع ثم يُجدد الاشتراك
    // يُستدعى من Controller مباشرة بعد عودة المخبر من صفحة MyFatoorah
    // ---------------------------------------------------------------
    public async Task<(bool Success, string Message)> VerifyAndFinalizeAsync(int userId, string paymentId)
    {
        // 1. جلب بيانات المخبر
        var lab = await _repo.GetLabWithPaymentsByUserIdAsync(userId);
        if (lab == null)
            return (false, "لم يتم العثور على ملف المخبر.");

        // 2. التحقق من حالة الدفع عبر MyFatoorah
        string apiKey = GetCleanValue(_config["MyFatoorah:ApiKey"] ?? "");
        string baseUrl = GetCleanValue(_config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com").TrimEnd('/');

        try
        {
            var verifyPayload = new { KeyType = "PaymentId", Key = paymentId };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var httpClient = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/GetPaymentStatus");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(verifyPayload);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (false, $"فشل التحقق من الدفع. رمز: {response.StatusCode}");

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
                return (false, "البوابة رفضت عملية التحقق.");

            var data = jsonResult.GetProperty("Data");
            string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
            string referenceStr = data.GetProperty("CustomerReference").GetString()!;
            decimal paidAmount = data.GetProperty("InvoiceValue").GetDecimal();

            string invoiceId = data.TryGetProperty("InvoiceId", out var invProp)
                ? invProp.GetInt32().ToString()
                : paymentId;

            // 3. التحقق من أن الدفع مكتمل وأن المرجع يخص اشتراك هذا المخبر
            if (!invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return (false, $"لم يكتمل الدفع بعد. الحالة: {invoiceStatus}");

            // referenceStr = "LSUB_{labId}_{months}"
            if (!referenceStr.StartsWith("LSUB_"))
                return (false, "هذه الفاتورة ليست خاصة باشتراك المخبر.");

            var parts = referenceStr.Split('_');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int refLabId) || !int.TryParse(parts[2], out int months))
                return (false, "مرجع الفاتورة غير صالح.");

            if (refLabId != lab.Id)
                return (false, "هذه الفاتورة لا تخص حسابك.");

            // 4. إتمام التجديد
            return await FinalizeSubscriptionAsync(lab.Id, paidAmount, months, invoiceId);
        }
        catch (Exception ex)
        {
            return (false, $"حدث خطأ أثناء التحقق: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------
    // VerifyAndFinalizeByLabIdAsync — نسخة بدون توكن (للـ PendingPayment)
    // تُستدعى من Controller عند التحقق بعد الدفع بدون authentication
    // ---------------------------------------------------------------
    public async Task<(bool Success, string Message)> VerifyAndFinalizeByLabIdAsync(int labId, string paymentId)
    {
        var lab = await _repo.GetLabWithPaymentsByLabIdAsync(labId);
        if (lab == null)
            return (false, "المخبر غير موجود.");

        string apiKey = GetCleanValue(_config["MyFatoorah:ApiKey"] ?? "");
        string baseUrl = GetCleanValue(_config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com").TrimEnd('/');

        try
        {
            var verifyPayload = new { KeyType = "PaymentId", Key = paymentId };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var httpClient = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/GetPaymentStatus");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(verifyPayload);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (false, $"فشل التحقق من الدفع. رمز: {response.StatusCode}");

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
                return (false, "البوابة رفضت عملية التحقق.");

            var data = jsonResult.GetProperty("Data");
            string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
            string referenceStr = data.GetProperty("CustomerReference").GetString()!;
            decimal paidAmount = data.GetProperty("InvoiceValue").GetDecimal();

            string invoiceId = data.TryGetProperty("InvoiceId", out var invProp)
                ? invProp.GetInt32().ToString()
                : paymentId;

            if (!invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return (false, $"لم يكتمل الدفع بعد. الحالة: {invoiceStatus}");

            if (!referenceStr.StartsWith("LSUB_"))
                return (false, "هذه الفاتورة ليست خاصة باشتراك مخبر.");

            var parts = referenceStr.Split('_');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int refLabId) || !int.TryParse(parts[2], out int months))
                return (false, "مرجع الفاتورة غير صالح.");

            if (refLabId != labId)
                return (false, "هذه الفاتورة لا تخص هذا المخبر.");

            return await FinalizeSubscriptionAsync(labId, paidAmount, months, invoiceId);
        }
        catch (Exception ex)
        {
            return (false, $"حدث خطأ أثناء التحقق: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------
    // FinalizeSubscriptionAsync — يُستدعى من MyFatoorahService.VerifyPaymentAsync
    // بعد نجاح الدفع: يُنشئ سجل LabSubscriptionPayment ويُجدد تواريخ المخبر
    // ---------------------------------------------------------------
    public async Task<(bool Success, string Message)> FinalizeSubscriptionAsync(
        int labId, decimal paidAmount, int months, string invoiceId)
    {
        var lab = await _repo.GetLabWithPaymentsByLabIdAsync(labId);
        if (lab == null)
            return (false, "المخبر غير موجود.");

        if (lab.Owner == null)
            return (false, "المستخدم المرتبط بهذا المخبر غير موجود.");

        var now = DateTime.UtcNow;
        var isCurrentlyActive = lab.SubscriptionEndUtc.HasValue && lab.SubscriptionEndUtc.Value > now;

        // البداية دائماً = اليوم (تاريخ الدفع)
        var periodStart = now;

        DateTime periodEnd;
        if (isCurrentlyActive)
        {
            // اشتراك نشط: نضيف الشهور الجديدة فوق النهاية الحالية
            // الأيام الباقية تتراكم مع الجديد
            periodEnd = lab.SubscriptionEndUtc!.Value.AddMonths(months);
        }
        else
        {
            // اشتراك منتهي أو ما عنده: من اليوم + الشهور الجديدة
            periodEnd = now.AddMonths(months);
        }

        // إضافة سجل الدفع — Amount = السعر لشهر واحد (للحفاظ على منطق السعر الشهري)
        var payment = new LabSubscriptionPayment
        {
            LabId = labId,
            Amount = paidAmount / months,
            Method = SubscriptionPaymentMethod.MyFatoorah,
            PaidAtUtc = now,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            Reference = $"MyFatoorah Invoice #{invoiceId} | {months} months"
        };

        await _repo.AddSubscriptionPaymentAsync(payment);

        // تحديث تواريخ الاشتراك على المخبر
        lab.SubscriptionStartUtc = periodStart;
        lab.SubscriptionEndUtc = periodEnd;

        // تفعيل الحساب دائماً بعد الدفع الناجح
        var wasPendingPayment = lab.Owner.Status == AccountStatus.PendingPayment;
        lab.Owner.Status = AccountStatus.Active;

        await _repo.UpdateLabAndUserAsync(lab, lab.Owner);

        // إرسال إيميل تأكيد
        if (!string.IsNullOrEmpty(lab.Owner.Email))
        {
            string subject;
            string body;

            if (wasPendingPayment)
            {
                // إيميل تفعيل الحساب للمرة الأولى
                subject = "تم تفعيل حسابك - مرحباً بك في منصة Dental Lab";
                body = $"مرحباً {lab.Owner.Name}،\n\n" +
                       $"يسعدنا إخبارك بأن عملية الدفع تمت بنجاح وتم تفعيل حسابك على منصة Dental Lab.\n\n" +
                       $"تفاصيل الاشتراك:\n" +
                       $"• المبلغ المدفوع: {paidAmount:N2}\n" +
                       $"• فترة الاشتراك: من {periodStart:yyyy-MM-dd} إلى {periodEnd:yyyy-MM-dd} ({months} شهر/شهور)\n" +
                       $"• رقم الفاتورة: #{invoiceId}\n\n" +
                       $"يمكنك الآن تسجيل الدخول والاستفادة من جميع خدمات المنصة.\n\n" +
                       $"شكراً لاستخدامك منصة Dental Lab.";
            }
            else
            {
                // إيميل تجديد اشتراك عادي
                subject = "تجديد اشتراك المخبر - تم الدفع بنجاح";
                body = $"مرحباً {lab.Owner.Name}،\n\n" +
                       $"تمت عملية الدفع الإلكتروني لاشتراكك بنجاح.\n" +
                       $"المبلغ المدفوع: {paidAmount:N2}\n" +
                       $"فترة الاشتراك الجديدة: من {periodStart:yyyy-MM-dd} إلى {periodEnd:yyyy-MM-dd} ({months} شهر/شهور).\n\n" +
                       $"رقم فاتورة MyFatoorah: #{invoiceId}\n\n" +
                       $"شكراً لاستخدامك منصة Dental Lab.";
            }

            await _emailService.SendEmailAsync(lab.Owner.Email, subject, body);
        }

        return (true, $"تم تجديد الاشتراك بنجاح حتى {periodEnd:yyyy-MM-dd}.");
    }

    // ---------------------------------------------------------------
    // VerifyAndFinalizeByPaymentIdAsync — يُستدعى من callback MyFatoorah
    // يستخرج labId من CustomerReference (LSUB_{labId}_{months}) تلقائياً
    // ---------------------------------------------------------------
    public async Task<(bool Success, string Message)> VerifyAndFinalizeByPaymentIdAsync(string paymentId)
    {
        string apiKey = GetCleanValue(_config["MyFatoorah:ApiKey"] ?? "");
        string baseUrl = GetCleanValue(_config["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com").TrimEnd('/');

        try
        {
            var verifyPayload = new { KeyType = "PaymentId", Key = paymentId };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var httpClient = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/GetPaymentStatus");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(verifyPayload);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (false, $"فشل التحقق من الدفع. رمز: {response.StatusCode}");

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!jsonResult.GetProperty("IsSuccess").GetBoolean())
                return (false, "البوابة رفضت عملية التحقق.");

            var data = jsonResult.GetProperty("Data");
            string invoiceStatus = data.GetProperty("InvoiceStatus").GetString()!;
            string referenceStr = data.GetProperty("CustomerReference").GetString()!;
            decimal paidAmount = data.GetProperty("InvoiceValue").GetDecimal();

            string invoiceId = data.TryGetProperty("InvoiceId", out var invProp)
                ? invProp.GetInt32().ToString()
                : paymentId;

            if (!invoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return (false, $"لم يكتمل الدفع. الحالة: {invoiceStatus}");

            // استخراج labId و months من LSUB_{labId}_{months}
            if (!referenceStr.StartsWith("LSUB_"))
                return (false, "هذه الفاتورة ليست لاشتراك مخبر.");

            var parts = referenceStr.Split('_');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int labId) || !int.TryParse(parts[2], out int months))
                return (false, "مرجع الفاتورة غير صالح.");

            return await FinalizeSubscriptionAsync(labId, paidAmount, months, invoiceId);
        }
        catch (Exception ex)
        {
            return (false, $"حدث خطأ: {ex.Message}");
        }
    }
}
