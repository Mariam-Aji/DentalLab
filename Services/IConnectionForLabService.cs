using DentalLab.Api.Models;
using System.Collections.Generic;

public interface IConnectionForLabService
{
    Task<(IEnumerable<ConnectionRequest> requests, string? error)> GetPendingRequestsForLabAsync(int labUserId);
    Task<string?> AcceptRequestAsync(int labUserId, int requestId);
    Task<string?> RejectRequestAsync(int labUserId, int requestId);
}
