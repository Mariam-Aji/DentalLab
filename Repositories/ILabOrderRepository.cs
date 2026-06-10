using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabOrderRepository
{
    Task<List<CaseOrder>> GetPendingOrdersForLabAsync(int labId);
    Task<int> GetPendingOrdersCountForLabAsync(int labId);
    Task<CaseOrder?> GetOrderWithFilesAsync(int orderId);
    Task UpdateOrderAsync(CaseOrder order);
}
