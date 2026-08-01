using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories.Interfaces;

namespace DentalLab.Api.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly ApplicationDbContext _context;

        public ComplaintRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // جلب شكاوى الإدارة من الجدول مباشرة بدون Includes
        public async Task<List<Complaint>> GetAdminComplaintsAsync()
        {
            return await _context.Complaints
                .Where(c => c.TargetLabId == null || c.Destination == ComplaintDestination.Admin)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
        }

        // جلب شكاوى المخابر من الجدول مباشرة بدون Includes
        public async Task<List<Complaint>> GetLabComplaintsAsync()
        {
            return await _context.Complaints
                .Where(c => c.Destination == ComplaintDestination.Lab && c.TargetLabId != null)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
        }

        // جلب شكاوى الطبيب من الجدول مباشرة بدون Includes
        public async Task<List<Complaint>> GetDentistComplaintsAsync(int dentistId)
        {
            return await _context.Complaints
                .Where(c => c.UserId == dentistId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
        }
        public async Task<Complaint?> GetByIdAsync(int id)
        {
            return await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}