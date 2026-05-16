using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface IAdminAccountsRepository
{
    Task<List<User>> GetPendingDentistApprovalsAsync();
    Task<List<User>> GetPendingLabApprovalsAsync();
    Task<User?> GetUserByIdTrackingAsync(int id);
    Task SaveChangesAsync();
}