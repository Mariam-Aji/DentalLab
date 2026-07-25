using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface ILabRepository
    {
        Task<IEnumerable<Lab>> GetAllLabsWithOwnersAsync();
        Task<IEnumerable<Lab>> GetLabsByAvailabilityAsync(AvailabilityStatus status);
        Task<List<int>> GetConnectedLabIdsForDentistAsync(int dentistId);
        Task<IEnumerable<Lab>> GetConnectedLabsForDentistAsync(int dentistId);
    }
}