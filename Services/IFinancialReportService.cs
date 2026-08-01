using System.Threading.Tasks;
using DentalLab.Api.Dtos.Reports;

namespace DentalLab.Api.Services.Interfaces
{
    public interface IFinancialReportService
    {
        Task<ConsolidatedFinancialReportDto> GenerateConsolidatedFinancialReportAsync();
    }
}