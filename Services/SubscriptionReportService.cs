using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Dtos.Reports;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Services.Interfaces;

namespace DentalLab.Api.Services
{
    public class SubscriptionReportService : ISubscriptionReportService
    {
        private readonly ISubscriptionReportRepository _repository;

        public SubscriptionReportService(ISubscriptionReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<SubscriptionStatusReportDto> GenerateSubscriptionReportAsync(int expiringDaysThreshold = 7)
        {
            var expiringSoon = await _repository.GetExpiringSoonLabsAsync(expiringDaysThreshold);
            var distribution = await _repository.GetLabsStatusDistributionAsync();
            var retention = await _repository.GetRetentionRateStatsAsync();

            return new SubscriptionStatusReportDto
            {
                ExpiringSoonLabs = expiringSoon,
                StatusDistribution = distribution,
                RetentionStats = retention
            };
        }
    }
}