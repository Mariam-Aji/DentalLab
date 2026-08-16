using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
namespace DentalLab.Api.Controllers;

/// <summary>
/// دفع وتجديد اشتراك المخبر إلكترونياً عبر MyFatoorah
///
/// الخطوات:
///   1. GET  /api/lab-subscription-online/price-info     → يعرض السعر الشهري وجدول الأسعار
///   2. POST /api/lab-subscription-online/pay            → يُنشئ رابط دفع MyFatoorah
///   3. بعد الدفع MyFatoorah يستدعي GET /api/payment/callback?paymentId=xxx (الموجود)
///      أو المخبر يستدعي مباشرة:
///      POST /api/lab-subscription-online/verify/{paymentId} → يُكمل التجديد
/// </summary>
[ApiController]
[Route("api/lab-subscription-online")]
[Authorize(Roles = "Lab")]
public class LabSubscriptionOnlineController : ControllerBase
{
    private readonly ILabSubscriptionOnlineService _service;
    private readonly IConfiguration _config;

    public LabSubscriptionOnlineController(ILabSubscriptionOnlineService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    // ------------------------------------------------------------------
    // GET /api/lab-subscription-online/my-status
    // المخبر يمرر التوكن ويرجع حالة اشتراكه: نشط/منتهي، فترة مجانية أم لا، تاريخ الانتهاء
    // ------------------------------------------------------------------
    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus()
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetMyStatusAsync(userId);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // GET /api/lab-subscription-online/price-info
    // يُرجع السعر الشهري + جدول الأسعار من 1 إلى 12 شهراً
    // ------------------------------------------------------------------
    [HttpGet("price-info")]
    public async Task<IActionResult> GetPriceInfo()
    {
        var userId = GetUserId();
        var (result, error) = await _service.GetPriceInfoAsync(userId);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // POST /api/lab-subscription-online/pay
    // المخبر يختار عدد الشهور → يُرجع رابط الدفع على MyFatoorah + تفاصيل السعر
    // ------------------------------------------------------------------
    [HttpPost("pay")]
    public async Task<IActionResult> InitiatePayment([FromForm] LabSubscriptionOnlineRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var (result, error) = await _service.InitiatePaymentAsync(userId, dto);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // GET /api/lab-subscription-online/callback
    // MyFatoorah يستدعي هذا تلقائياً بعد الدفع (SubscriptionCallbackUrl)
    // يتحقق من الدفع ويجدد الاشتراك ثم يحول للـ frontend
    // ------------------------------------------------------------------
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SubscriptionCallback([FromQuery] string paymentId)
    {
        string successUrl = _config["PaymentRedirects:SuccessUrl"] ?? "https://yourfrontend.com/payment-success";
        string failedUrl  = _config["PaymentRedirects:FailedUrl"]  ?? "https://yourfrontend.com/payment-failed";

        if (string.IsNullOrEmpty(paymentId))
            return Redirect($"{failedUrl}?error={Uri.EscapeDataString("معرف الدفع غير صالح.")}");

        var result = await _service.VerifyAndFinalizeByPaymentIdAsync(paymentId);

        if (result.Success)
            return Redirect($"{successUrl}?paymentId={paymentId}");

        return Redirect($"{failedUrl}?paymentId={paymentId}&error={Uri.EscapeDataString(result.Message)}");
    }

    // ------------------------------------------------------------------
    // GET /api/lab-subscription-online/pending-price-info?labId=x&userId=y
    // جدول الأسعار للمخبر PendingPayment — بدون توكن
    // ------------------------------------------------------------------
    [HttpGet("pending-price-info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPendingPriceInfo([FromQuery] int labId, [FromQuery] int userId)
    {
        var (result, error) = await _service.GetPendingPriceInfoAsync(labId, userId);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // POST /api/lab-subscription-online/pending-pay
    // للمخبر الذي حسابه PendingPayment — بدون توكن
    // يبعث labId + userId + عدد الشهور → يرجع رابط دفع MyFatoorah
    // بعد الدفع: حسابه يصير Active وتصله رسالة تفعيل على إيميله
    // ------------------------------------------------------------------
    [HttpPost("pending-pay")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiatePendingPayment([FromForm] PendingPaymentInitiateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (result, error) = await _service.InitiatePendingPaymentAsync(dto);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // POST /api/lab-subscription-online/verify/{paymentId}
    // يُستدعى بعد عودة المخبر من MyFatoorah للتحقق من الدفع وتجديد الاشتراك
    // AllowAnonymous لأن المخبر PendingPayment ما عنده توكن بعد
    // ------------------------------------------------------------------
    [HttpPost("verify/{paymentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyAndRenew(string paymentId, [FromQuery] int? labId)
    {
        if (string.IsNullOrEmpty(paymentId))
            return BadRequest(new { message = "معرّف الدفع مطلوب." });

        // إذا عنده توكن (مخبر نشط يجدد) نستخدم userId من التوكن
        // إذا ما عنده توكن (PendingPayment) لازم يمرر labId
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId();
            var verifyResult = await _service.VerifyAndFinalizeAsync(userId, paymentId);
            if (!verifyResult.Success)
                return BadRequest(new { message = verifyResult.Message });
            return Ok(new { message = verifyResult.Message });
        }
        else
        {
            if (!labId.HasValue)
                return BadRequest(new { message = "labId مطلوب عند التحقق بدون توكن." });

            var verifyResult = await _service.VerifyAndFinalizeByLabIdAsync(labId.Value, paymentId);
            if (!verifyResult.Success)
                return BadRequest(new { message = verifyResult.Message });
            return Ok(new { message = verifyResult.Message });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException();
        return id;
    }
}
