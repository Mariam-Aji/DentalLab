using DentalLab.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [Authorize(Roles = "Dentist")]
    [HttpPost("case/{caseOrderId}/add-patient")]
    public async Task<IActionResult> AddPatientToCase([FromRoute] int caseOrderId, [FromForm] Patient patientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int dentistId))
            {
                return Unauthorized(new { message = "التوكن غير صالح أو منتهي الصلاحية." });
            }

            var createdPatient = await _patientService.CreatePatientForCaseAsync(dentistId, caseOrderId, patientDto);

            return Ok(new
            {
                message = "تم إنشاء سجل المريض وربطه بالطلبية بنجاح.",
                patientId = createdPatient.Id,
                patientName = createdPatient.FullName
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ غير متوقع في السيرفر.", error = ex.Message });
        }
    }
}