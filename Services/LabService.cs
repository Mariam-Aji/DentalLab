using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class LabService : ILabService
    {
        private readonly ILabRepository _labRepository;

        public LabService(ILabRepository labRepository)
        {
            _labRepository = labRepository;
        }

        public async Task<IEnumerable<LabDto>> GetLabsSummaryAsync()
        {
            var labs = await _labRepository.GetAllLabsWithOwnersAsync();
            return MapToSummaryDto(labs);
        }

        public async Task<IEnumerable<LabDto>> GetConnectedLabsAsync()
        {
            var labs = await _labRepository.GetLabsByAvailabilityAsync(AvailabilityStatus.Available);
            return MapToSummaryDto(labs);
        }

        public async Task<IEnumerable<LabDto>> GetDisconnectedLabsAsync()
        {
            var allLabs = await _labRepository.GetAllLabsWithOwnersAsync();
            var disconnected = allLabs.Where(l => l.Availability != AvailabilityStatus.Available);
            return MapToSummaryDto(disconnected);
        }

        private IEnumerable<LabDto> MapToSummaryDto(IEnumerable<Lab> labs)
        {
            return labs.Select(l => new LabDto
            {
                Id = l.Id,
                Name = l.Owner != null ? l.Owner.NamePlace : "Unknown Lab Name"
            }).ToList();
        }
    }
}