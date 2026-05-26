using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public interface ILabService
    {
        Task<IEnumerable<LabDto>> GetLabsSummaryAsync();
        Task<IEnumerable<LabDto>> GetConnectedLabsAsync();
        Task<IEnumerable<LabDto>> GetDisconnectedLabsAsync();
    }
}
