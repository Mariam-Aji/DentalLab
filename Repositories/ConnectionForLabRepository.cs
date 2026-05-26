using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DentalLab.Api.Repositories
{
    public class ConnectionForLabRepository : IConnectionForLabRepository
    {
        private readonly ApplicationDbContext _context;

        public ConnectionForLabRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetLabIdByUserAsync(int userId)
        {
            return await _context.Labs
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ConnectionRequest>> GetPendingRequestsForLabAsync(int labId)
        {
            return await _context.ConnectionRequests
                .Include(r => r.FromDentist)
                .Where(r => r.ToLabId == labId && r.Status == ConnectionRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingRequestsCountForLabAsync(int labId)
        {
            return await _context.ConnectionRequests
                .Where(r => r.ToLabId == labId && r.Status == ConnectionRequestStatus.Pending)
                .CountAsync();
        }

        public async Task<ConnectionRequest?> GetRequestForLabAsync(int requestId, int labId)
        {
            return await _context.ConnectionRequests
                .Include(r => r.FromDentist)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ToLabId == labId);
        }

        public async Task<bool> UpdateRequestStatusAsync(ConnectionRequest request, ConnectionRequestStatus status)
        {
            request.Status = status;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
