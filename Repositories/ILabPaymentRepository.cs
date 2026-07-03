using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface ILabPaymentRepository
    {
        Task<CaseOrder?> GetTargetOrderAsync(int orderId);
        Task<OrderInvoice?> GetInvoiceByOrderIdAsync(int orderId);
        //Task<OrderInvoice?> GetInvoiceByGatewayReferenceAsync(string gatewayId);
        Task SaveInvoiceAsync(OrderInvoice invoice);
        Task CommitInvoiceChangesAsync(OrderInvoice invoice);
        Task UpdateOrderStatusAsync(CaseOrder order);
    }
}