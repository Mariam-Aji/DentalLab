using DentalLab.Api.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public interface IStatisticsRepository
{
    Task<List<LabMonthlyOrdersDto>> GetLabMonthlyOrdersStatisticsAsync();
    Task<List<DentistMonthlyOrdersDto>> GetDentistMonthlyOrdersStatisticsAsync();
    //
}