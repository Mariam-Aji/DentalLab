using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Dtos.Reports;

namespace DentalLab.Api.Repositories.Interfaces
{
    public interface ISubscriptionReportRepository
    {
        Task<List<ExpiringLabDto>> GetExpiringSoonLabsAsync(int daysThreshold);
        Task<LabsStatusDistributionDto> GetLabsStatusDistributionAsync();
        Task<RetentionRateReportDto> GetRetentionRateStatsAsync();
    }
}