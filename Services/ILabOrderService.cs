using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabOrderService
{
    Task<List<LabPendingOrderDto>> GetPendingOrdersForLabAsync(int labId);
    Task<int> GetPendingOrdersCountForLabAsync(int labId);
    Task<(object? result, string? error)> ApproveOrderAsync(int orderId, int labId);
    Task<(object? result, string? error)> RejectOrderAsync(int orderId, int labId, string reason);
    Task<(object? result, string? error)> RequestMoreInfoAsync(int orderId, int labId, string message);
}
