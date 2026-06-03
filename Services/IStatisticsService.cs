using DentalLab.Api.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public interface IStatisticsService
{
    Task<List<LabMonthlyOrdersDto>> GetLabMonthlyOrdersChartDataAsync();
    Task<List<DentistMonthlyOrdersDto>> GetDentistMonthlyOrdersChartDataAsync();
}