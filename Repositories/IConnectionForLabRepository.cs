using DentalLab.Api.Models;
using System.Collections.Generic;
//
public interface IConnectionForLabRepository
{
    Task<int?> GetLabIdByUserAsync(int userId);
    Task<List<ConnectionRequest>> GetPendingRequestsForLabAsync(int labId);
    Task<int> GetPendingRequestsCountForLabAsync(int labId);
    Task<ConnectionRequest?> GetRequestForLabAsync(int requestId, int labId);
    Task<bool> UpdateRequestStatusAsync(ConnectionRequest request, ConnectionRequestStatus status);
}
