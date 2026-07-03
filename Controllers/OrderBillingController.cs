//using System.Security.Claims;
//using System.Threading.Tasks;
//using DentalLab.Api.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace DentalLab.Api.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class OrderBillingController : ControllerBase
//    {
//        //private readonly IOrderBillingService _billingService;

//        //public OrderBillingController(IOrderBillingService billingService)
//        //{
//        //    _billingService = billingService;
//        //}

//        [Authorize]
//        [HttpPost("checkout/order/{orderId}")]
//        public async Task<IActionResult> StartCheckout(int orderId)
//        {
//            var identityClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            if (!int.TryParse(identityClaim, out int dentistId))
//            {
//                return Unauthorized(new { message = "جلسة العمل الحالية للطبيب غير صالحة أو منتهية." });
//            }

//            var currentDomain = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

//            var (checkoutUrl, errorId) = await _billingService.InitializeOrderCheckoutAsync(orderId, dentistId, currentDomain);

//            if (!string.IsNullOrEmpty(errorId))
//            {
//                return BadRequest(new { message = errorId });
//            }

//            return Ok(new { paymentUrl = checkoutUrl });
//        }

//        [HttpGet("verify")]
//        public async Task<IActionResult> VerifyTransaction([FromQuery] string paymentId)
//        {
//            var (isVerified, failureReason) = await _billingService.VerifyAndCompletePaymentAsync(paymentId);
//            if (!isVerified)
//            {
//                return Redirect($"http://localhost:3000/payment-status?success=false&reason={failureReason}");
//            }
//            return Redirect("http://localhost:3000/payment-status?success=true");
//        }

//        [HttpGet("decline")]
//        public IActionResult CheckoutDeclined([FromQuery] string orderId)
//        {
//            return Redirect($"http://localhost:3000/payment-status?success=false&orderId={orderId}");
//        }
//    }
//}