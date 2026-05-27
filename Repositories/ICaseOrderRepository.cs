using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ICaseOrderRepository
{
    Task<CaseOrder> CreateOrderAsync(CaseOrder order);
    Task<CaseOrder?> GetOrderByIdAsync(int orderId);
    Task UpdateOrderAsync(CaseOrder order);
    Task AddOrderItemAsync(CaseOrderItem item);
    Task<LabPrice?> GetUnitPriceAsync(int labId, CompensationType type);
    Task<bool> IsDentistConnectedToLab(int dentistId, int labId);
    Task<CaseOrder?> GetOrderWithItemsAsync(int orderId);
    Task<LabPrice?> GetLabPriceAsync(int labId, CompensationType type);
    Task<bool> AddPatientAndBindToOrderAsync(CaseOrder order, Patient patient);
    Task<List<Patient>> GetAllPatientsAsync();
    Task<Patient?> GetPatientByIdAsync(int patientId);
    Task<Patient?> GetPatientWithFilesByIdAsync(int patientId);
    Task<bool> UpdatePatientAsync(Patient patient);

}