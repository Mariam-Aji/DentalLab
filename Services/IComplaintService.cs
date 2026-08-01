using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Dtos.Complaints;
using DentalLab.Api.Models;

namespace DentalLab.Api.Services.Interfaces
{
    public interface IComplaintService
    {
        Task<ComplaintResponseDto> CreateComplaintAsync(int userId, int? targetLabId, CreateComplaintDto dto);
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        Task<List<ComplaintDetailsDto>> GetAdminComplaintsAsync();
        Task<List<ComplaintDetailsDto>> GetLabComplaintsAsync();
        Task<List<ComplaintDetailsDto>> GetDentistComplaintsAsync(int dentistId);
        Task<ComplaintDetailsDto> ReplyToComplaintAsync(int dentistId, int complaintId, ReplyComplaintDto dto);
    }
}