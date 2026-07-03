using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabOrderService : ILabOrderService
{
    private readonly ILabOrderRepository _repo;

    public LabOrderService(ILabOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<LabPendingOrderDto>> GetPendingOrdersForLabAsync(int labId)
    {
        var orders = await _repo.GetPendingOrdersForLabAsync(labId);

        var result = orders.Select(co => new LabPendingOrderDto
        {
            OrderId = co.Id,
            Title = co.Title,
            Status = co.Status.ToString(),
            ImpressionStage = co.ImpressionStage.ToString(),
            ImpressionType = co.ImpressionType.ToString(),
            Shade = co.Shade,
            IsTemporary = co.IsTemporary,
            IsUrgent = co.IsUrgent,
            DeliveryDate = co.DeliveryDate,
            Notes = co.Notes,
            EstimatedPrice = co.EstimatedPrice,
            FinalPrice = co.FinalPrice,
            IsPaid = co.IsPaid,
            CreatedAt = co.CreatedAt,
            HasAccessories = co.HasAccessories,

            DentistId = co.CreatedById,
            DentistName = co.CreatedBy?.Name ?? "",
            DentistEmail = co.CreatedBy?.Email ?? "",
            DentistPhone = co.CreatedBy?.Phone,
            DentistClinicAddress = co.CreatedBy?.AddressPlace,

            LabId = co.AssignedLabId,

            Items = co.Items.Select(item => new OrderDetailsItemDto
            {
                ItemId = item.Id,
                CompensationType = item.CompensationType.ToString(),
                ToothNumbers = item.ToothNumbers
            }).ToList(),

            RequiredImages = co.RequiredImages ?? new List<string>(),

            Files = co.Files.Select(f => new FileDto
            {
                Id = f.Id,
                Path = f.Path,
                Type = f.Type.ToString(),
                UploadedAt = f.UploadedAt
            }).ToList()
        }).ToList();

        return result;
    }

    public async Task<int> GetPendingOrdersCountForLabAsync(int labId)
    {
        return await _repo.GetPendingOrdersCountForLabAsync(labId);
    }

    public async Task<(object? result, string? error)> ApproveOrderAsync(int orderId, int labId)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null) return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.Accepted;
        await _repo.UpdateOrderAsync(order);

        return (new { message = "تم قبول الطلب من قبل المخبر." }, null);
    }

    public async Task<(object? result, string? error)> RejectOrderAsync(int orderId, int labId, string reason)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null) return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.Cancelled;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? reason : order.Notes + "\n" + reason;
        await _repo.UpdateOrderAsync(order);

        return (new { message = "تم رفض الطلب من قبل المخبر." }, null);
    }

    public async Task<(object? result, string? error)> RequestMoreInfoAsync(int orderId, int labId, string message)
    {
        var order = await _repo.GetOrderWithFilesAsync(orderId);
        if (order == null) return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        order.Status = CaseStatus.RequestInfo;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? message : order.Notes + "\n" + message;
        await _repo.UpdateOrderAsync(order);

        return (new { message = "تم طلب معلومات إضافية من الطبيب." }, null);
    }
}
