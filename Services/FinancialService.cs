using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class FinancialService : IFinancialService
{
    private readonly IFinancialRepository _financialRepository;

    public FinancialService(IFinancialRepository financialRepository)
    {
        _financialRepository = financialRepository;
    }

    public async Task<List<MonthlyFinancialGrowthDto>> GetFinancialGrowthPerMonthAsync(int year)
    {
        return await _financialRepository.GetFinancialGrowthPerMonthAsync(year);
    }
}