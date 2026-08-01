using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabComplaintService
{
    /// <summary>جلب الشكاوى الواردة للمخبر مرتبة من الأحدث</summary>
    Task<(List<LabComplaintDto>? result, string? error)> GetLabComplaintsAsync(int userId);

    /// <summary>جلب تفاصيل شكوى محددة</summary>
    Task<(LabComplaintDto? result, string? error)> GetComplaintByIdAsync(int userId, int complaintId);

    /// <summary>رد المخبر على شكوى + إشعار الطبيب</summary>
    Task<(LabComplaintDto? result, string? error)> ReplyToComplaintAsync(int userId, int complaintId, LabComplaintReplyDto dto);
}
