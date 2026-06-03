using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("labs-monthly-orders")]
    public async Task<IActionResult> GetLabsMonthlyOrders()
    {
        try
        {
            var data = await _statisticsService.GetLabMonthlyOrdersChartDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب إحصائيات المخابر.", error = ex.Message });
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("dentists-monthly-orders")]
    public async Task<IActionResult> GetDentistsMonthlyOrders()
    {
        try
        {
            var data = await _statisticsService.GetDentistMonthlyOrdersChartDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب إحصائيات طلبات الأطباء.", error = ex.Message });
        }
    }
}
//