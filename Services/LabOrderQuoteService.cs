using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabOrderQuoteService : ILabOrderQuoteService
{
    private readonly ILabOrderQuoteRepository _repo;

    // ترجمة عربية لأنواع التعويضات
    private static readonly Dictionary<CompensationType, string> _compensationAr = new()
    {
        { CompensationType.Veneer,         "فينير" },
        { CompensationType.ZirconCrown,    "تاج زيركون" },
        { CompensationType.ImplantCrown,   "تاج زراعة" },
        { CompensationType.Bridge,         "جسر" },
        { CompensationType.FullDenture,    "طقم كامل" },
        { CompensationType.PartialDenture, "طقم جزئي" },
        { CompensationType.Other,          "أخرى" },
    };

    public LabOrderQuoteService(ILabOrderQuoteRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// يبني عرض السعر (كفاتورة) للمخبر بناءً على أسعاره المسجلة لكل نوع تعويض.
    /// </summary>
    public async Task<(OrderQuoteDto? quote, string? error)> GetOrderQuoteAsync(int orderId, int labId)
    {
        var order = await _repo.GetOrderWithItemsAndPatientAsync(orderId);
        if (order == null) return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        var labPrices = await _repo.GetLabPricesAsync(labId);
        var priceMap  = labPrices.ToDictionary(p => p.CompensationType, p => p.UnitPrice);

        // نجمّع العناصر حسب نوع التعويض
        var grouped = order.Items
            .GroupBy(i => i.CompensationType)
            .Select(g =>
            {
                var allTeeth = g.SelectMany(i => i.ToothNumbers).Distinct().OrderBy(t => t).ToList();
                // إذا ما في أرقام أسنان نعدّ عدد العناصر
                var qty      = allTeeth.Count > 0 ? allTeeth.Count : g.Count();
                var hasPrice = priceMap.TryGetValue(g.Key, out var unitPrice);

                return new OrderQuoteLineDto
                {
                    CompensationType   = g.Key.ToString(),
                    CompensationTypeAr = _compensationAr.GetValueOrDefault(g.Key, g.Key.ToString()),
                    Quantity           = qty,
                    ToothNumbers       = allTeeth,
                    UnitPrice          = hasPrice ? unitPrice : 0,
                    LineTotal          = hasPrice ? unitPrice * qty : 0,
                    PriceFound         = hasPrice,
                };
            })
            .ToList();

        var estimatedTotal = grouped.Sum(l => l.LineTotal);

        // نحفظ الـ EstimatedPrice في الطلبية إذا اختلف
        if (order.EstimatedPrice != estimatedTotal)
        {
            order.EstimatedPrice = estimatedTotal;
            await _repo.UpdateOrderAsync(order);
        }

        var quote = new OrderQuoteDto
        {
            OrderId        = order.Id,
            OrderTitle     = order.Title,
            DentistName    = order.CreatedBy?.Name ?? "",
            DeliveryDate   = order.DeliveryDate,
            IsUrgent       = order.IsUrgent,
            Lines          = grouped,
            EstimatedTotal = estimatedTotal,
            FinalPrice     = order.FinalPrice,
            IsPaid         = order.IsPaid,
            Notes          = order.Notes,
        };

        return (quote, null);
    }

    /// <summary>
    /// المخبر يدخل السعر النهائي للطلبية.
    /// </summary>
    public async Task<(object? result, string? error)> SetFinalPriceAsync(int orderId, int labId, decimal finalPrice)
    {
        if (finalPrice <= 0) return (null, "السعر النهائي يجب أن يكون أكبر من صفر.");

        var order = await _repo.GetOrderWithItemsAndPatientAsync(orderId);
        if (order == null) return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        if (order.IsPaid)
            return (null, "لا يمكن تعديل السعر النهائي بعد أن قام الطبيب بالدفع.");

        order.FinalPrice = finalPrice;
        await _repo.UpdateOrderAsync(order);

        return (new
        {
            message    = "تم تحديد السعر النهائي بنجاح.",
            orderId    = order.Id,
            finalPrice = order.FinalPrice
        }, null);
    }
}
