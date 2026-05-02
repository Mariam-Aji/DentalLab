using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface IAdminAccountService
{
    Task<List<User>> GetPendingDentistApprovalsAsync();
    Task<string?> ApproveDentistAsync(int id);
    Task<string?> RejectDentistAsync(int id);
    Task<string?> SuspendDentistAsync(int id);
    Task<List<User>> GetPendingLabApprovalsAsync();
    Task<string?> ApproveLabAsync(int id);
    Task<string?> RejectLabAsync(int id);
    Task<string?> SuspendLabAsync(int id);
}