using DentalLab.Api.Models;
using System;
using System.Collections.Generic;

namespace DentalLab.Api.Services
{
    public class ConnectionForLabService : IConnectionForLabService
    {
        private readonly IConnectionForLabRepository _connectionForLabRepository;

        public ConnectionForLabService(IConnectionForLabRepository connectionForLabRepository)
        {
            _connectionForLabRepository = connectionForLabRepository;
        }

        public async Task<(IEnumerable<ConnectionRequest> requests, string? error)> GetPendingRequestsForLabAsync(int labUserId)
        {
            var labId = await _connectionForLabRepository.GetLabIdByUserAsync(labUserId);
            if (labId == null) return (Array.Empty<ConnectionRequest>(), "المخبر غير موجود.");

            var requests = await _connectionForLabRepository.GetPendingRequestsForLabAsync(labId.Value);
            return (requests, null);
        }

        public async Task<string?> AcceptRequestAsync(int labUserId, int requestId)
        {
            var labId = await _connectionForLabRepository.GetLabIdByUserAsync(labUserId);
            if (labId == null) return "المخبر غير موجود.";

            var request = await _connectionForLabRepository.GetRequestForLabAsync(requestId, labId.Value);
            if (request == null) return "الطلب غير موجود.";
            if (request.Status != ConnectionRequestStatus.Pending) return "تمت معالجة الطلب مسبقاً.";

            var updated = await _connectionForLabRepository.UpdateRequestStatusAsync(request, ConnectionRequestStatus.Accepted);
            return updated ? null : "فشل في تنفيذ العملية.";
        }

        public async Task<string?> RejectRequestAsync(int labUserId, int requestId)
        {
            var labId = await _connectionForLabRepository.GetLabIdByUserAsync(labUserId);
            if (labId == null) return "المخبر غير موجود.";

            var request = await _connectionForLabRepository.GetRequestForLabAsync(requestId, labId.Value);
            if (request == null) return "الطلب غير موجود.";
            if (request.Status != ConnectionRequestStatus.Pending) return "تمت معالجة الطلب مسبقاً.";

            var updated = await _connectionForLabRepository.UpdateRequestStatusAsync(request, ConnectionRequestStatus.Rejected);
            return updated ? null : "فشل في تنفيذ العملية.";
        }
    }
}
