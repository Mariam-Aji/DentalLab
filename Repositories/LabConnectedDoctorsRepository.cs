using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class LabConnectedDoctorsRepository : ILabConnectedDoctorsRepository
{
    private readonly ApplicationDbContext _context;

    public LabConnectedDoctorsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetConnectedDoctorsAsync(int labId)
    {
        return await _context.ConnectionRequests
            .Where(r => r.ToLabId == labId && r.Status == ConnectionRequestStatus.Accepted)
            .Include(r => r.FromDentist)
            .OrderBy(r => r.FromDentist.Name)
            .Select(r => r.FromDentist)
            .ToListAsync();
    }

    public async Task<List<CaseOrder>> GetOrdersByDentistForLabAsync(int labId, int dentistId)
    {
        return await _context.CaseOrders
            .Include(co => co.Items)
            .Include(co => co.Files)
            .Include(co => co.CreatedBy)
            .Where(co => co.AssignedLabId == labId && co.CreatedById == dentistId)
            .OrderBy(co => co.DeliveryDate == null)
            .ThenBy(co => co.DeliveryDate)
            .ThenByDescending(co => co.CreatedAt)
            .ToListAsync();
    }

    public async Task<ConnectionRequest?> GetAcceptedConnectionAsync(int labId, int dentistId)
    {
        return await _context.ConnectionRequests
            .FirstOrDefaultAsync(r =>
                r.ToLabId == labId &&
                r.FromDentistId == dentistId &&
                r.Status == ConnectionRequestStatus.Accepted);
    }

    public async Task DeleteConnectionAsync(ConnectionRequest connection)
    {
        _context.ConnectionRequests.Remove(connection);
        await _context.SaveChangesAsync();
    }
}
