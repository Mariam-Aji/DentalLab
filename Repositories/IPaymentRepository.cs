using System.Threading.Tasks;
using DentalLab.Api.Models;

public interface IPaymentRepository
{
    Task<CaseOrder?> GetOrderWithUserAsync(int orderId);
    Task<bool> UpdateOrderPaymentStatusAsync(int orderId, decimal paidAmount, bool isPaid);
}