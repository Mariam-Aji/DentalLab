using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabOrderQuoteRepository
{
    Task<CaseOrder?> GetOrderWithItemsAndPatientAsync(int orderId);
    Task<List<LabPrice>> GetLabPricesAsync(int labId);
    Task UpdateOrderAsync(CaseOrder order);
}
