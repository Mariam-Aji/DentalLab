using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public interface ILabSubscriptionService
    {
        Task<(bool Success, string Message)> CreateLabSubscriptionAsync(int labId, CreateSubscriptionDto dto);
        Task<IEnumerable<ActiveLabDto>> GetActiveSubscribedLabsAsync();
        Task<(bool Success, string Message)> UpdateSubscriptionInfoAsync(int labId, UpdateSubscriptionDto dto);
        Task<(bool Success, string Message)> RenewSubscriptionAsync(int labId, RenewSubscriptionDto dto);
    }
}