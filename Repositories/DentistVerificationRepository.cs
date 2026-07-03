using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class DentistVerificationRepository : IDentistVerificationRepository
    {
        private readonly ApplicationDbContext _context;

        public DentistVerificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetPendingDentistAccountsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Dentist &&
                           (u.Status == AccountStatus.PendingAdminApproval || u.Status == AccountStatus.PendingVerification))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserStatusAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}