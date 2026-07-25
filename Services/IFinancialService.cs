using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface IFinancialService
{
    Task<List<MonthlyFinancialGrowthDto>> GetFinancialGrowthPerMonthAsync(int year);
}