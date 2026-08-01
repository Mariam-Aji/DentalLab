using System.Threading.Tasks;
using DentalLab.Api.Models;

public interface IPaymentRepository
{
    Task<CaseOrder?> GetOrderWithUserAndLabAsync(int orderId);
    Task<CaseOrder?> UpdateOrderPaymentStatusAsync(int orderId, decimal paidAmount, bool isPaid);
    Task<Advertisement?> GetAdvertisementWithUserAsync(int adId);
    Task<Advertisement?> UpdateAdPaymentStatusAsync(int adId, decimal paidAmount, bool isPaid);
}