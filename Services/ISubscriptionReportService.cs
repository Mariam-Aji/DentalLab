using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Dtos.Reports;

namespace DentalLab.Api.Services.Interfaces
{
    public interface ISubscriptionReportService
    {
        Task<SubscriptionStatusReportDto> GenerateSubscriptionReportAsync(int expiringDaysThreshold = 7);
    }
}