using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Repositories;

public interface IFinancialRepository
{
    Task<List<MonthlyFinancialGrowthDto>> GetFinancialGrowthPerMonthAsync(int year);
}