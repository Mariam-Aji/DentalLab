using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IMyFatoorahService _myFatoorahService;
    private readonly IConfiguration _config;

    public PaymentController(IMyFatoorahService myFatoorahService, IConfiguration config)
    {
        _myFatoorahService = myFatoorahService;
        _config = config;
    }

  
    [HttpPost("pay-order/{caseOrderId}")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> PayOrder(int caseOrderId, [FromQuery] string currency = "USD")
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int doctorId))
            {
                return Unauthorized(new { message = "فشل التحقق من الهوية الشخصية للطبيب." });
            }

            var result = await _myFatoorahService.ProcessOrderPaymentAsync(caseOrderId, doctorId, currency);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(new
            {
                paymentLink = result.PaymentUrl,
                message = "تم إنشاء رابط الدفع بنجاح."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ غير متوقع أثناء معالجة الطلب.", detail = ex.Message });
        }
    }

   
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromQuery] string paymentId)
    {
        if (string.IsNullOrEmpty(paymentId))
        {
            string fallbackErrorUrl = _config["PaymentRedirects:FailedUrl"] ?? "https://yourfrontend.com/payment-failed";
            return Redirect($"{fallbackErrorUrl}?error={Uri.EscapeDataString("رقم معرف الدفع غير صالح.")}");
        }

        var result = await _myFatoorahService.VerifyPaymentAsync(paymentId);

        string successUrl = _config["PaymentRedirects:SuccessUrl"] ?? "https://yourfrontend.com/payment-success";
        string failedUrl = _config["PaymentRedirects:FailedUrl"] ?? "https://yourfrontend.com/payment-failed";

        if (result.Success)
        {
            return Redirect($"{successUrl}?paymentId={paymentId}");
        }

        return Redirect($"{failedUrl}?paymentId={paymentId}&error={Uri.EscapeDataString(result.Error ?? "فشلت عملية الدفع.")}");
    }

   
    [HttpGet("verify/{paymentId}")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment(string paymentId)
    {
        if (string.IsNullOrEmpty(paymentId))
            return BadRequest(new { message = "معرف الدفع مطلوب." });

        var result = await _myFatoorahService.VerifyPaymentAsync(paymentId);

        if (result.Success)
        {
            return Ok(new { isPaid = true, status = result.Status, message = "تمت عملية الدفع وتحديث الطلب بنجاح." });
        }

        return BadRequest(new { isPaid = false, status = result.Status, message = result.Error });
    }
}