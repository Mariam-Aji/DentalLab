using System.Security.Claims;
using System.Threading.Tasks;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IBillingService _billingService;

    public InvoicesController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out int id) ? id : 0;
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("paid/orders")]
    public async Task<IActionResult> GetPaidOrders()
    {
        int dentistId = GetCurrentUserId();
        var orders = await _billingService.GetPaidOrdersAsync(dentistId);

        if (orders == null || !orders.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد طلبات مدفوعة حالياً.",
                count = 0,
                data = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الطلبات المدفوعة بنجاح.",
            count = orders.Count,
            data = orders
        });
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("paid/advertisements")]
    public async Task<IActionResult> GetPaidAdvertisements()
    {
        int userId = GetCurrentUserId();
        var ads = await _billingService.GetPaidAdvertisementsAsync(userId);

        if (ads == null || !ads.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد إعلانات مدفوعة حالياً.",
                count = 0,
                data = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الإعلانات المدفوعة بنجاح.",
            count = ads.Count,
            data = ads
        });
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("paid/all")]
    public async Task<IActionResult> GetPaidOrdersAndAdvertisements()
    {
        int userId = GetCurrentUserId();
        var (orders, ads) = await _billingService.GetPaidOrdersAndAdvertisementsAsync(userId);

        bool hasAny = (orders != null && orders.Any()) || (ads != null && ads.Any());

        if (!hasAny)
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد فواتير مدفوعة (طلبات أو إعلانات) حالياً.",
                ordersCount = 0,
                adsCount = 0,
                ordersData = new List<object>(),
                adsData = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الفواتير المدفوعة بنجاح.",
            ordersCount = orders.Count,
            adsCount = ads.Count,
            ordersData = orders,
            adsData = ads
        });
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("unpaid/orders")]
    public async Task<IActionResult> GetUnpaidOrders()
    {
        int dentistId = GetCurrentUserId();
        var orders = await _billingService.GetUnpaidOrdersAsync(dentistId);

        if (orders == null || !orders.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد طلبات غير مدفوعة حالياً.",
                count = 0,
                data = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الطلبات غير المدفوعة بنجاح.",
            count = orders.Count,
            data = orders
        });
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("unpaid/advertisements")]
    public async Task<IActionResult> GetUnpaidAdvertisements()
    {
        int userId = GetCurrentUserId();
        var ads = await _billingService.GetUnpaidAdvertisementsAsync(userId);

        if (ads == null || !ads.Any())
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد إعلانات غير مدفوعة حالياً.",
                count = 0,
                data = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الإعلانات غير المدفوعة بنجاح.",
            count = ads.Count,
            data = ads
        });
    }
    [Authorize(Roles = "Dentist")]

    [HttpGet("unpaid/all")]
    public async Task<IActionResult> GetUnpaidOrdersAndAdvertisements()
    {
        int userId = GetCurrentUserId();
        var (orders, ads) = await _billingService.GetUnpaidOrdersAndAdvertisementsAsync(userId);

        bool hasAny = (orders != null && orders.Any()) || (ads != null && ads.Any());

        if (!hasAny)
        {
            return Ok(new
            {
                success = true,
                message = "لا توجد فواتير غير مدفوعة (طلبات أو إعلانات) حالياً.",
                ordersCount = 0,
                adsCount = 0,
                ordersData = new List<object>(),
                adsData = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم جلب الفواتير غير المدفوعة بنجاح.",
            ordersCount = orders.Count,
            adsCount = ads.Count,
            ordersData = orders,
            adsData = ads
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/advertisements/paid")]
   
    public async Task<IActionResult> GetGroupedPaidAdInvoices()
    {
        var (dentists, labs, adsClients) = await _billingService.GetGroupedPaidAdInvoicesAsync();

        bool hasData = dentists.Any() || labs.Any() || adsClients.Any();

        return Ok(new
        {
            success = true,
            message = hasData ? "تم جلب فواتير الإعلانات المدفوعة بنجاح." : "لا توجد فواتير إعلانات مدفوعة حالياً.",
            data = new
            {
                dentists,
                labs,
                adsClients
            }
        });
    }

    /// <summary>
    /// عرض فواتير الإعلانات غير المدفوعة مقسمة بحسب المالك (أطباء ومخابر فقط)
    /// </summary>
    ///   
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/advertisements/unpaid")]
 
    public async Task<IActionResult> GetGroupedUnpaidAdInvoices()
    {
        var (dentists, labs) = await _billingService.GetGroupedUnpaidAdInvoicesAsync();

        bool hasData = dentists.Any() || labs.Any();

        return Ok(new
        {
            success = true,
            message = hasData ? "تم جلب فواتير الإعلانات غير المدفوعة بنجاح." : "لا توجد فواتير إعلانات غير مدفوعة حالياً.",
            data = new
            {
                dentists,
                labs
            }
        });
    }
}
