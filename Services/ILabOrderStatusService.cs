using DentalLab.Api.Dtos;
using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface ILabOrderStatusService
{
    /// <summary>عدد الطلبات لكل حالة خاصة بالمخبر</summary>
    Task<List<OrderStatusCountDto>> GetOrdersCountByStatusAsync(int labId);

    /// <summary>جلب الطلبيات بالتفصيل حسب حالة معينة</summary>
    Task<(List<LabPendingOrderDto>? orders, string? error)> GetOrdersByStatusAsync(
        int labId, CaseStatus status);

    /// <summary>جلب تفاصيل طلبية واحدة تخص المخبر</summary>
    Task<(LabPendingOrderDto? order, string? error)> GetOrderByIdAsync(int orderId, int labId);
    Task<(object? result, string? error)> UpdateOrderStatusAsync(
        int orderId, int labId, UpdateOrderStatusDto dto, string uploadsRootPath);
}
