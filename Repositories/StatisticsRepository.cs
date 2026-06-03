using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly ApplicationDbContext _context;

    public StatisticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LabMonthlyOrdersDto>> GetLabMonthlyOrdersStatisticsAsync()
    {
        return await _context.CaseOrders
            .Where(co => co.AssignedLabId != null) 
            .GroupBy(co => new
            {
                co.AssignedLabId,
                LabName = co.AssignedLab!.Owner.NamePlace ?? co.AssignedLab.Owner.Name,
                co.CreatedAt.Year,
                co.CreatedAt.Month
            })
            .Select(g => new LabMonthlyOrdersDto
            {
                LabId = g.Key.AssignedLabId!.Value,
                LabName = g.Key.LabName,
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalOrders = g.Count() 
            })
            .OrderBy(r => r.LabName)
            .ThenBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToListAsync();
    }
    public async Task<List<DentistMonthlyOrdersDto>> GetDentistMonthlyOrdersStatisticsAsync()
    {
        return await _context.CaseOrders
            .GroupBy(co => new
            {
                co.CreatedById,
                DentistName = co.CreatedBy!.Name,
                co.CreatedAt.Year,
                co.CreatedAt.Month
            })
            .Select(g => new DentistMonthlyOrdersDto
            {
                DentistId = g.Key.CreatedById,
                DentistName = g.Key.DentistName, 
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalOrders = g.Count()
            })
            .OrderBy(r => r.DentistName)
            .ThenBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToListAsync();
    }
}