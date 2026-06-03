using DentalLab.Api.Dtos;
using DentalLab.Api.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _repository;

    public StatisticsService(IStatisticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<LabMonthlyOrdersDto>> GetLabMonthlyOrdersChartDataAsync()
    {
        return await _repository.GetLabMonthlyOrdersStatisticsAsync();
    }
    public async Task<List<DentistMonthlyOrdersDto>> GetDentistMonthlyOrdersChartDataAsync()
    {
        return await _repository.GetDentistMonthlyOrdersStatisticsAsync();
    }
    //
}