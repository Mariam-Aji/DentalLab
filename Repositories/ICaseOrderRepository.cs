using DentalLab.Api.Dtos;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ICaseOrderRepository
{
    Task<CaseOrder> CreateOrderAsync(CaseOrder order);
    Task<CaseOrder?> GetOrderByIdAsync(int orderId);
    //Task UpdateOrderAsync(CaseOrder order);
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
    Task<List<CaseOrderDetailDto>> GetAllCaseOrdersWithDetailsAsync();
    Task<bool> AddCaseOrderItemsRangeAsync(List<CaseOrderItem> items);
    //
    Task SaveNotificationAsync(Notification notification);
    Task<bool> DeleteOrderAsync(CaseOrder order);
    Task<Lab?> GetLabByIdAsync(int labId);
    Task<OrderInvoice?> GetInvoiceByOrderIdAsync(int orderId);
    Task AddInvoiceAsync(OrderInvoice invoice);
    Task<List<CaseOrder>> GetDentistOrdersWithItemsAsync(int dentistId);
    Task<List<OrderInvoice>> GetInvoicesByOrderIdsAsync(List<int> orderIds);
    //Task AddInvoicesRangeAsync(List<OrderInvoice> invoices);
    Task UpdateInvoiceAsync(OrderInvoice invoice);
    //Task<OrderInvoice?> GetInvoiceByMyFatoorahIdAsync(int mfInvoiceId);
    // داخل واجهة المستودع ICaseOrderRepository أو الواجهة الخاصة بالفواتير
    Task AddInvoicesRangeAsync(List<OrderInvoice> invoices);
    Task UpdateOrderAsync(CaseOrder order);
}