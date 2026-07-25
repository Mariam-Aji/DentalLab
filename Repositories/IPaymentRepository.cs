using System.Threading.Tasks;
using DentalLab.Api.Models;

public interface IPaymentRepository
{
    Task<CaseOrder?> GetOrderWithUserAndLabAsync(int orderId);
    Task<bool> UpdateOrderPaymentStatusAsync(int orderId, decimal paidAmount, bool isPaid);
    Task<Advertisement?> GetAdvertisementWithUserAsync(int adId);
    Task<bool> UpdateAdPaymentStatusAsync(int adId, decimal paidAmount, bool isPaid);
}
