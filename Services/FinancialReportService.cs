using System.Threading.Tasks;
using DentalLab.Api.Dtos.Reports;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Services.Interfaces;

namespace DentalLab.Api.Services
{
    public class FinancialReportService : IFinancialReportService
    {
        private readonly IFinancialReportRepository _reportRepository;

        public FinancialReportService(IFinancialReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<ConsolidatedFinancialReportDto> GenerateConsolidatedFinancialReportAsync()
        {
            var adsStats = await _reportRepository.GetPaidAdvertisementsStatsAsync();

            var subsStats = await _reportRepository.GetActiveSubscriptionsStatsAsync();

            return new ConsolidatedFinancialReportDto
            {
                PaidAdsTotalRevenue = adsStats.TotalRevenue,
                PaidAdsCount = adsStats.Count,

                ActiveSubscriptionsTotalRevenue = subsStats.TotalRevenue,
                ActiveSubscriptionsCount = subsStats.Count
            };
        }
    }
}