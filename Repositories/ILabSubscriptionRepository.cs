using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface ILabSubscriptionRepository
    {
        Task<Lab?> GetLabWithUserAsync(int labId);
        Task AddSubscriptionPaymentAsync(LabSubscriptionPayment payment);
        Task UpdateLabAndUserAsync(Lab lab, User user);
        Task<IEnumerable<Lab>> GetActiveSubscribedLabsAsync();
        Task<IEnumerable<Lab>> GetExpiredLabsAsync();
        Task UpdateLabsRangeAsync(IEnumerable<Lab> labs);
        Task<LabSubscriptionPayment?> GetLatestPaymentAsync(int labId);
        Task UpdateSubscriptionPaymentAsync(LabSubscriptionPayment payment);
    }
}