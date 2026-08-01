using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalLab.Api.Services.Interfaces;
using DentalLab.Api.Dtos.Reports;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IFinancialReportService _financialReportService;
        private readonly ISubscriptionReportService _subscriptionReportService;

        // حقن الخدمتين المالية وحالة الاشتراكات في الباني
        public ReportsController(
            IFinancialReportService financialReportService,
            ISubscriptionReportService subscriptionReportService)
        {
            _financialReportService = financialReportService;
            _subscriptionReportService = subscriptionReportService;
        }

        /// <summary>
        /// جلب التقرير المالي المجمع للإعلانات المدفوعة والاشتراكات النشطة
        /// </summary>
        [HttpGet("financial/consolidated")]
        public async Task<ActionResult<ConsolidatedFinancialReportDto>> GetConsolidatedFinancialReport()
        {
            try
            {
                var report = await _financialReportService.GenerateConsolidatedFinancialReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ داخلي أثناء توليد التقرير المالي: {ex.Message}");
            }
        }

        /// <summary>
        /// جلب تقرير حالة المخابر والاشتراكات (المخابر القريبة من الانتهاء، التوزيع، ومعدل الاحتفاظ)
        /// </summary>
        /// <param name="days">عدد الأيام المتبقية للانتهاء (الافتراضي 7 أيام)</param>
        [HttpGet("subscriptions/status-report")]
        public async Task<ActionResult<SubscriptionStatusReportDto>> GetSubscriptionStatusReport([FromQuery] int days = 7)
        {
            try
            {
                var report = await _subscriptionReportService.GenerateSubscriptionReportAsync(days);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ أثناء توليد تقرير الاشتراكات: {ex.Message}");
            }
        }
    }
}