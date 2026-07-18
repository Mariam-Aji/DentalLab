using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IMyFatoorahService _myFatoorahService;

    public PaymentController(IMyFatoorahService myFatoorahService)
    {
        _myFatoorahService = myFatoorahService;
    }

    [HttpPost("pay-order/{caseOrderId}")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> PayOrder(int caseOrderId, [FromQuery] string currency = "USD")
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int doctorId))
            {
                return Unauthorized(new { message = "فشل التحقق من الهوية الشخصية للطبيب." });
            }

            var result = await _myFatoorahService.ProcessOrderPaymentAsync(caseOrderId, doctorId, currency);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(new { paymentLink = result.PaymentUrl, message = "تم إنشاء رابط الدفع بنجاح." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> PaymentCallback([FromQuery] string paymentId)
    {
        var result = await _myFatoorahService.VerifyPaymentAsync(paymentId);

        if (result.Success)
        {
            return Redirect("https://yourfrontend.com/payment-success");
        }

        return Redirect($"https://yourfrontend.com/payment-failed?error={result.Error}");
    }
}