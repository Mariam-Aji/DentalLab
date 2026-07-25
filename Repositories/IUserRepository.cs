using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface IUserRepository
{
    Task<List<User>> SearchUsersForAdminAsync(string searchTerm);
    Task<List<User>> GetAllDentistsAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> ChangeUserStatusAsync(int userId, AccountStatus newStatus);

}