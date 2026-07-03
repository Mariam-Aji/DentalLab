using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public interface IDentistVerificationService
    {
        Task<IEnumerable<PendingDentistDto>> GetPendingDentistsOnlyAsync();
        Task<(bool Success, string Message)> SuspendDentistAccountAsync(int userId);
    }
}