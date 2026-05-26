using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class AdminAccountService : IAdminAccountService
{
    private readonly IAdminAccountsRepository _repo;

    public AdminAccountService(IAdminAccountsRepository repo)
    {
        _repo = repo;
    }

    public Task<List<User>> GetPendingDentistApprovalsAsync()
        => _repo.GetPendingDentistApprovalsAsync();

    public Task<List<User>> GetPendingLabApprovalsAsync()
        => _repo.GetPendingLabApprovalsAsync();

    public Task<string?> ApproveDentistAsync(int id)
        => SetDentistStatusAsync(id, AccountStatus.Active, requirePendingAdminApproval: true);

    public Task<string?> RejectDentistAsync(int id)
        => SetDentistStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: true);

    public Task<string?> SuspendDentistAsync(int id)
        => SetDentistStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: false);

    public Task<string?> ApproveLabAsync(int id)
        => SetLabStatusAsync(id, AccountStatus.Active, requirePendingAdminApproval: true);

    public Task<string?> RejectLabAsync(int id)
        => SetLabStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: true);

    public Task<string?> SuspendLabAsync(int id)
        => SetLabStatusAsync(id, AccountStatus.Suspended, requirePendingAdminApproval: false);

    private async Task<string?> SetDentistStatusAsync(int id, AccountStatus status, bool requirePendingAdminApproval)
    {
        var user = await _repo.GetUserByIdTrackingAsync(id);
        if (user == null) return "User not found.";
        if (user.Role != UserRole.Dentist) return "User is not a dentist.";
        if (requirePendingAdminApproval && user.Status != AccountStatus.PendingAdminApproval)
        {
            return "Dentist is not pending admin approval.";
        }

        if (user.Status == status) return "Dentist already has this status.";

        user.Status = status;
        await _repo.SaveChangesAsync();
        return null;
    }

    private async Task<string?> SetLabStatusAsync(int id, AccountStatus status, bool requirePendingAdminApproval)
    {
        var user = await _repo.GetUserByIdTrackingAsync(id);
        if (user == null) return "User not found.";
        if (user.Role != UserRole.Lab) return "User is not a lab.";
        if (requirePendingAdminApproval && user.Status != AccountStatus.PendingAdminApproval)
        {
            return "Lab is not pending admin approval.";
        }

        if (user.Status == status) return "Lab already has this status.";

        user.Status = status;
        await _repo.SaveChangesAsync();
        return null;
    }
}