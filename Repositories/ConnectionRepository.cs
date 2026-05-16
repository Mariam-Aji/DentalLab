using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class ConnectionRepository : IConnectionRepository
    {
        private readonly ApplicationDbContext _context;
        public ConnectionRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> CreateRequestAsync(ConnectionRequest request)
        {
            await _context.ConnectionRequests.AddAsync(request);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RequestExistsAsync(int dentistId, int labId)
        {
            return await _context.ConnectionRequests
                .AnyAsync(c => c.FromDentistId == dentistId && c.ToLabId == labId);
        }
        public async Task<bool> LabExistsAsync(int labId)
        {
            return await _context.Labs.AnyAsync(l => l.Id == labId);
        }

        public async Task<bool> DeleteRequestAsync(int dentistId, int labId)
        {
            var request = await _context.ConnectionRequests
                .FirstOrDefaultAsync(c => c.FromDentistId == dentistId && c.ToLabId == labId);

            if (request == null) return false;

            _context.ConnectionRequests.Remove(request);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}