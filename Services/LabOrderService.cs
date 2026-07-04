using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabOrderService : ILabOrderService
{
    private readonly ILabOrderRepository  _repo;
    private readonly INotificationService _notifications;

    public LabOrderService(ILabOrderRepository repo, INotificationService notifications)
    {
        _repo          = repo;
        _notifications = notifications;
    }

    public async Task<List<LabPendingOrderDto>> GetPendingOrdersForLabAsync(int labId)
    {
        var orders = await _repo.GetPendingOrdersForLabAsync(labId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<int> GetPendingOrdersCountForLabAsync(int labId)
        => await _repo.GetPendingOrdersCountForLabAsync(labId);

    // ----------------------------------------------------------------
    // قبول الطلب
    // ----------------------------------------------------------------
    public async Task<(object? result, string? error)> ApproveOrderAsync(int orderId, int labId)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null)                   return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId)    return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.Accepted;
        await _repo.UpdateOrderAsync(order);

        // إشعار الطبيب بقبول طلبيته
        await _notifications.SendAsync(
            recipientUserId: order.CreatedById,
            message: $"تم قبول طلبيتك رقم ({orderId}) من قبل المخبر.",
            type: NotificationType.OrderAccepted,
            orderId: orderId,
            labId: labId);

        return (new { message = "تم قبول الطلب من قبل المخبر." }, null);
    }

    // ----------------------------------------------------------------
    // رفض الطلب
    // ----------------------------------------------------------------
    public async Task<(object? result, string? error)> RejectOrderAsync(int orderId, int labId, string reason)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null)                   return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId)    return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.Cancelled;
        order.Notes  = string.IsNullOrWhiteSpace(order.Notes) ? reason : order.Notes + "\n" + reason;
        await _repo.UpdateOrderAsync(order);

        // إشعار الطبيب برفض طلبيته
        await _notifications.SendAsync(
            recipientUserId: order.CreatedById,
            message: $"تم رفض طلبيتك رقم ({orderId}) من قبل المخبر. السبب: {reason}",
            type: NotificationType.OrderRejected,
            orderId: orderId,
            labId: labId);

        return (new { message = "تم رفض الطلب من قبل المخبر." }, null);
    }

    // ----------------------------------------------------------------
    // طلب معلومات إضافية
    // ----------------------------------------------------------------
    public async Task<(object? result, string? error)> RequestMoreInfoAsync(int orderId, int labId, string message)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null)                   return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId)    return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.RequestInfo;
        order.Notes  = string.IsNullOrWhiteSpace(order.Notes) ? message : order.Notes + "\n" + message;
        await _repo.UpdateOrderAsync(order);

        // إشعار الطبيب بطلب المعلومات الإضافية
        await _notifications.SendAsync(
            recipientUserId: order.CreatedById,
            message: $"المخبر يطلب معلومات إضافية بخصوص طلبيتك رقم ({orderId}): {message}",
            type: NotificationType.InfoRequested,
            orderId: orderId,
            labId: labId);

        return (new { message = "تم طلب معلومات إضافية من الطبيب." }, null);
    }

    // ---- helper ----
    private static LabPendingOrderDto MapToDto(CaseOrder co) => new()
    {
        OrderId              = co.Id,
        Title                = co.Title,
        Status               = co.Status.ToString(),
        ImpressionStage      = co.ImpressionStage.ToString(),
        ImpressionType       = co.ImpressionType.ToString(),
        Shade                = co.Shade,
        IsTemporary          = co.IsTemporary,
        IsUrgent             = co.IsUrgent,
        DeliveryDate         = co.DeliveryDate,
        Notes                = co.Notes,
        EstimatedPrice       = co.EstimatedPrice,
        FinalPrice           = co.FinalPrice,
        IsPaid               = co.IsPaid,
        CreatedAt            = co.CreatedAt,
        HasAccessories       = co.HasAccessories,
        DentistId            = co.CreatedById,
        DentistName          = co.CreatedBy?.Name ?? "",
        DentistEmail         = co.CreatedBy?.Email ?? "",
        DentistPhone         = co.CreatedBy?.Phone,
        DentistClinicAddress = co.CreatedBy?.AddressPlace,
        LabId                = co.AssignedLabId,
        Items = co.Items.Select(i => new OrderDetailsItemDto
        {
            ItemId           = i.Id,
            CompensationType = i.CompensationType.ToString(),
            ToothNumbers     = i.ToothNumbers
        }).ToList(),
        RequiredImages = co.RequiredImages ?? new List<string>(),
        Files = co.Files.Select(f => new FileDto
        {
            Id         = f.Id,
            Path       = f.Path,
            Type       = f.Type.ToString(),
            UploadedAt = f.UploadedAt
        }).ToList()
    };
}
