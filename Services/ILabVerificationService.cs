using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public interface ILabVerificationService
    {
        Task<IEnumerable<PendingLabDto>> GetPendingLabsOnlyAsync();
        Task<(bool Success, string Message)> SuspendLabAccountAsync(int userId);
    }
}