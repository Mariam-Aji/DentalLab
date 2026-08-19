using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Dentist,Admin")]
public class CaseOrdersController : ControllerBase
{
    private readonly ICaseOrderService _service;
    private readonly IWebHostEnvironment _env;

    public CaseOrdersController(ICaseOrderService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

    [HttpPost("initiate/{labId}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateOrderHead(
        [FromForm] CreateCaseOrderDto dto,
        int labId)
    {
        try
        {
            int dentistId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (result, error) =
                await _service.CreateInitialOrderAsync(dto, dentistId, labId);

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{orderId}/add-item")]
    public async Task<IActionResult> AddItemToOrder(
      int orderId,
      [FromForm] CaseOrderItemDto dto)
    {
        try
        {
            int dentistId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result =
                await _service.AddItemToOrderAsync(orderId, dto, dentistId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Dentist")]
    [HttpGet("my-invoices")]
    public async Task<IActionResult> GetDentistInvoices()
    {
        try
        {
            // استخراج معرف الطبيب من التوكن
            var dentistIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(dentistIdClaim))
            {
                return Unauthorized(new { message = "لم يتم العثور على صلاحيات الطبيب في التوكن." });
            }

            int dentistId = int.Parse(dentistIdClaim);

            // استدعاء السيرفس لجلب ومعالجة كافة فواتير هذا الطبيب
            var invoices = await _service.GetOrCreateDentistInvoicesAsync(dentistId);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{caseOrderId}/add-patient")]
    public async Task<IActionResult> AddPatientToOrder(int caseOrderId, [FromForm] CreatePatientDto patientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 🌟 تمرير الـ _env مع البارامترات
        var (result, error) = await _service.AddPatientToCaseOrderAsync(caseOrderId, patientDto, _env);

        if (error != null)
        {
            if (error.Contains("غير موجودة"))
            {
                return NotFound(new { message = error });
            }
            return BadRequest(new { message = error });
        }

        return Ok(result);
    }
    [HttpGet("patients")]
    public async Task<IActionResult> GetAllPatients()
    {
        try
        {
            var patients = await _service.GetAllPatientsAsync();
            return Ok(patients);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{caseOrderId}/bind-patient/{patientId}")]
    public async Task<IActionResult> BindPatientToOrder(int caseOrderId, int patientId)
    {
        try
        {
            var result = await _service.BindExistingPatientToOrderAsync(caseOrderId, patientId);

            var messageProp = result.GetType().GetProperty("message")?.GetValue(result, null) as string;
            if (messageProp != null && messageProp.Contains("غير موجود"))
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
   
    [HttpPut("patient/{patientId}/update")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePatient(int patientId, [FromForm] UpdatePatientDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            int dentistId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (result, error) = await _service.UpdatePatientDetailsAsync(patientId, dto, dentistId);

            if (error != null)
            {
                if (error.Contains("غير موجود")) return NotFound(new { message = error });
                return BadRequest(new { message = error });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("all-with-details")]
    public async Task<IActionResult> GetAllOrdersWithDetails()
    {
        try
        {
            var orders = await _service.GetAllOrdersWithDetailsAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب قائمة الطلبيات الشاملة.", error = ex.Message });
        }

    }
    [Authorize(Roles = "Dentist")]
    [HttpPut("{caseOrderId}/lab/{labId}/add-items")]
    [Consumes("multipart/form-data")] 
    public async Task<IActionResult> AddItemsToOrder(int caseOrderId, int labId, [FromForm] AddCaseOrderItemsDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            (bool success, string? error) = await _service.AddItemsToExistingOrderAsync(caseOrderId, labId, dto);
            if (!success) return BadRequest(new { message = error });

            return Ok(new { message = "تمت إضافة العناصر بنجاح باستخدام FromForm وتوجيه الإشعار!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    [Authorize(Roles = "Dentist")]
    [HttpDelete("{caseOrderId}/lab/{labId}/cancel")]
    public async Task<IActionResult> CancelOrder(int caseOrderId, int labId, [FromForm] CancelCaseOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (success, message, refundAmount) = await _service.CancelAndProcessOrderAsync(caseOrderId, labId, dto);
            if (!success) return BadRequest(new { message });

            return Ok(new
            {
                message = "تم إلغاء الطلبية بنجاح وحذفها وتوجيه الإشعار للمخبر.",
                details = message,
                refundAmount = refundAmount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }

    }
    [HttpGet("my-orders-tracking")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetMyOrdersTracking()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new { Message = "جلسة العمل منتهية أو غير صالحة." });

        int dentistId = int.Parse(userIdClaim.Value);

        var orders = await _service.GetDentistOrdersTrackingAsync(dentistId);

        return Ok(orders);
    }
    [HttpGet("lab/{labId}/orders")]
    [Authorize(Roles = "Dentist")] 
    public async Task<IActionResult> GetMyOrdersWithSpecificLab(int labId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new { Message = "جلسة العمل غير صالحة، يرجى إعادة تسجيل الدخول." });

        int dentistId = int.Parse(userIdClaim.Value);

        var orders = await _service.GetOrdersByDentistAndLabAsync(dentistId, labId);

        return Ok(orders);
    }
    [HttpGet("dentist-personal-profile")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetDentistPersonalProfile()
    {
        int dentistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var profile = await _service.FetchDentistPersonalProfileAsync(dentistId);
        if (profile == null)
            return NotFound(new { Message = "لم يتم العثور على البيانات." });

        return Ok(profile);
    }

    // 2️⃣ تابع تعديل البيانات الشخصية الفريد
    [HttpPut("edit-personal-profile")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> EditDentistPersonalProfile([FromForm] EditDentistOwnProfileDto dto)
    {
        int dentistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var (profile, error) = await _service.ModifyDentistPersonalProfileAsync(dentistId, dto);

        if (error != null)
            return BadRequest(new { Message = error });

        return Ok(new
        {
            Message = "تم تحديث البيانات بنجاح.",
            Data = profile
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("compensations-chart")]
    public async Task<IActionResult> GetCompensationsChartData()
    {
        var chartData = await _service.GetCompensationDemandChartDataAsync();

        return Ok(new
        {
            count = chartData.Count,
            data = chartData
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("status-chart")]
    public async Task<IActionResult> GetCaseStatusChartData()
    {
        var chartData = await _service.GetCaseStatusChartDataAsync();

        return Ok(new
        {
            count = chartData.Count,
            data = chartData
        });
    }
    [Authorize(Roles = "Dentist")]
    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetOrdersByPatient(int patientId)
    {
        var orders = await _service.GetOrdersByPatientIdAsync(patientId);

        if (!orders.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد طلبيات مرتبطة بهذا المريض حالياً.",
                data = orders
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب طلبيات المريض بنجاح.",
            data = orders
        });
    }
    [Authorize(Roles = "Dentist")]
    [HttpGet("with-patients")]
    public async Task<IActionResult> GetOrdersWithPatients()
    {
        var orders = await _service.GetOrdersWithPatientsAsync();

        if (!orders.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد طلبيات مرتبطة بمرضى حالياً.",
                data = orders
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الطلبيات المربوطة بالمرضى بنجاح.",
            data = orders
        });
    }
    [HttpPost("{orderId}/send-notification-to-lab/{labId}")]
    public async Task<IActionResult> SendOrderDetailsNotification(int orderId, int labId)
    {
        try
        {
            var dentistClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(dentistClaim) || !int.TryParse(dentistClaim, out int dentistId))
            {
                return Unauthorized(new { message = "غير مصرح لك، أو الـ Token غير صالح." });
            }

            // تنفيذ التابع
            var resultMessage = await _service.SendFullOrderNotificationToLabAsync(orderId, labId, dentistId);

            if (resultMessage.Contains("غير موجودة") || resultMessage.Contains("لا تخص هذا") || resultMessage.Contains("غير مرسلة") || resultMessage.Contains("لم يتم العثور"))
            {
                return BadRequest(new { message = resultMessage });
            }

            return Ok(new { message = resultMessage });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ داخلي في الخادم.", details = ex.Message });
        }
    }
}

    
