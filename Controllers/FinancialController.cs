using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FinancialController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public FinancialController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("growth/{year}")]
    public async Task<IActionResult> GetFinancialGrowth(int year)
    {
        if (year < 2000 || year > 2100)
        {
            return BadRequest(new { message = "السنة المدخلة غير صالحة." });
        }

        var financialGrowthData = await _financialService.GetFinancialGrowthPerMonthAsync(year);

        return Ok(new
        {
            year = year,
            totalMonths = financialGrowthData.Count,
            data = financialGrowthData
        });
    }
}