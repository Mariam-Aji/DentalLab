using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabOrderQuoteService : ILabOrderQuoteService
{
    private readonly ILabOrderQuoteRepository _repo;
    private readonly INotificationService     _notifications;

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

    public LabOrderQuoteService(ILabOrderQuoteRepository repo, INotificationService notifications)
    {
        _repo          = repo;
        _notifications = notifications;
    }

    public async Task<(OrderQuoteDto? quote, string? error)> GetOrderQuoteAsync(int orderId, int labId)
    {
        var order = await _repo.GetOrderWithItemsAndPatientAsync(orderId);
        if (order == null)                return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        var labPrices = await _repo.GetLabPricesAsync(labId);
        var priceMap  = labPrices.ToDictionary(p => p.CompensationType, p => p.UnitPrice);

        var grouped = order.Items
            .GroupBy(i => i.CompensationType)
            .Select(g =>
            {
                var allTeeth = g.SelectMany(i => i.ToothNumbers).Distinct().OrderBy(t => t).ToList();
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

        if (order.EstimatedPrice != estimatedTotal)
        {
            order.EstimatedPrice = estimatedTotal;
            await _repo.UpdateOrderAsync(order);
        }

        return (new OrderQuoteDto
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
        }, null);
    }

    // ----------------------------------------------------------------
    // تحديد السعر النهائي + إشعار الطبيب
    // ----------------------------------------------------------------
    public async Task<(object? result, string? error)> SetFinalPriceAsync(
        int orderId, int labId, decimal finalPrice)
    {
        if (finalPrice <= 0) return (null, "السعر النهائي يجب أن يكون أكبر من صفر.");

        var order = await _repo.GetOrderWithItemsAndPatientAsync(orderId);
        if (order == null)                return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");
        if (order.IsPaid)                 return (null, "لا يمكن تعديل السعر النهائي بعد أن قام الطبيب بالدفع.");

        order.FinalPrice = finalPrice;
        await _repo.UpdateOrderAsync(order);

        // إشعار الطبيب بتحديد السعر النهائي
        await _notifications.SendAsync(
            recipientUserId: order.CreatedById,
            message: $"تم تحديد السعر النهائي لطلبيتك رقم ({orderId}): {finalPrice:N2} ل.س",
            type: NotificationType.PriceSet,
            orderId: orderId,
            labId: labId);

        return (new
        {
            message    = "تم تحديد السعر النهائي بنجاح.",
            orderId    = order.Id,
            finalPrice = order.FinalPrice
        }, null);
    }
}
