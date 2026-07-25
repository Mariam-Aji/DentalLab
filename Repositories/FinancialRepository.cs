using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class FinancialRepository : IFinancialRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MonthlyFinancialGrowthDto>> GetFinancialGrowthPerMonthAsync(int year)
    {
        // 1. جلب المدفوعات المدفوعة للإعلانات خلال السنة المحددة
        var adsData = await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.IsPaid && a.CreatedAt.Year == year)
            .Select(a => new { Month = a.CreatedAt.Month, Amount = a.Price ?? 0 })
            .ToListAsync();

        // 2. جلب المدفوعات المدفوعة للطلبيات خلال السنة المحددة
        // (نعتمد على FinalPrice وإذا لم يكن موجوداً نأخذ EstimatedPrice)
        var ordersData = await _context.CaseOrders
            .AsNoTracking()
            .Where(o => o.IsPaid && o.CreatedAt.Year == year)
            .Select(o => new { Month = o.CreatedAt.Month, Amount = o.FinalPrice ?? o.EstimatedPrice ?? 0 })
            .ToListAsync();

        // 3. دمج البيانات وتشكيل الأشهر الـ 12 (لضمان ظهور حتى الأشهر التي ليس لها مدفوعات بقيمة 0)
        var result = new List<MonthlyFinancialGrowthDto>();

        for (int month = 1; month <= 12; month++)
        {
            decimal adsTotal = adsData.Where(x => x.Month == month).Sum(x => x.Amount);
            decimal ordersTotal = ordersData.Where(x => x.Month == month).Sum(x => x.Amount);

            // الحصول على اسم الشهر اختصاراً (مثل Jan, Feb أو بالعربية حسب ثقافة الخادم)
            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);

            result.Add(new MonthlyFinancialGrowthDto
            {
                Month = month,
                MonthName = monthName,
                AdsRevenue = adsTotal,
                OrdersRevenue = ordersTotal,
                TotalRevenue = adsTotal + ordersTotal
            });
        }

        return result;
    }
}