using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Repositories
{
    public class LabRepository : ILabRepository
    {
        private readonly ApplicationDbContext _context;
        public LabRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Lab>> GetAllLabsWithOwnersAsync()
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lab>> GetLabsByAvailabilityAsync(AvailabilityStatus status)
        {
            return await _context.Labs
                .Include(l => l.Owner)
                .Where(l => l.Availability == status)
                .ToListAsync();
        }
    }
}