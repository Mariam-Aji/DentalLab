using System.Threading.Tasks;

namespace DentalLab.Api.Repositories.Interfaces
{
    public interface IFinancialReportRepository
    {
        Task<(decimal TotalRevenue, int Count)> GetPaidAdvertisementsStatsAsync();
        Task<(decimal TotalRevenue, int Count)> GetActiveSubscriptionsStatsAsync();
    }
}