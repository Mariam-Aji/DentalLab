using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface ILabVerificationRepository
    {
        Task<IEnumerable<User>> GetPendingLabAccountsAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task UpdateUserStatusAsync(User user);
    }
}