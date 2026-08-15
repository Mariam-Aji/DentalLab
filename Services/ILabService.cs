using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.DTOs;

namespace DentalLab.Api.Services
{
    public interface ILabService
    {
        Task<IEnumerable<LabDto>> GetLabsSummaryAsync(int? currentDentistId = null);
        Task<IEnumerable<LabDto>> GetConnectedLabsAsync(int? currentDentistId = null);
        Task<IEnumerable<LabDto>> GetDisconnectedLabsAsync(int? currentDentistId = null);
        Task<List<AdminLabDto>> GetAllLabsForAdminAsync();
        Task<int?> GetLabIdByUserIdAsync(int userId);
    }
}