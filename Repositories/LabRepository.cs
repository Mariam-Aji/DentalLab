using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class LabRepository : ILabRepository
    {
        private readonly ApplicationDbContext _context;

        public LabRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Lab>> GetAllLabsWithOwnersAsync()
        {
            return await _context.Labs
                .AsNoTracking()
                .Include(l => l.Owner)
                .Include(l => l.Ratings)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lab>> GetLabsByAvailabilityAsync(AvailabilityStatus status)
        {
            return await _context.Labs
                .AsNoTracking()
                .Include(l => l.Owner)
                .Include(l => l.Ratings)
                .Where(l => l.Availability == status)
                .ToListAsync();
        }

        public async Task<List<int>> GetConnectedLabIdsForDentistAsync(int dentistId)
        {
            return await _context.ConnectionRequests
                .AsNoTracking()
                .Where(cr => cr.FromDentistId == dentistId && cr.Status == ConnectionRequestStatus.Accepted)
                .Select(cr => cr.ToLabId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lab>> GetConnectedLabsForDentistAsync(int dentistId)
        {
            return await _context.ConnectionRequests
                .AsNoTracking()
                .Where(cr => cr.FromDentistId == dentistId && cr.Status == ConnectionRequestStatus.Accepted)
                .Include(cr => cr.ToLab)
                    .ThenInclude(l => l.Owner)
                .Include(cr => cr.ToLab)
                    .ThenInclude(l => l.Ratings)
                .Select(cr => cr.ToLab)
                .ToListAsync();
        }
    }
}