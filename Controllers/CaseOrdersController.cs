using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Dentist")]
public class CaseOrdersController : ControllerBase
{
    private readonly ICaseOrderService _service;

    public CaseOrdersController(ICaseOrderService service)
    {
        _service = service;
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

    [HttpGet("{orderId}/invoice")]
    public async Task<IActionResult> GetInvoice(int orderId)
    {
        try
        {
            var result = await _service.GetOrderInvoiceAsync(orderId);
            return Ok(result);
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

        var (result, error) = await _service.AddPatientToCaseOrderAsync(caseOrderId, patientDto);

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
}