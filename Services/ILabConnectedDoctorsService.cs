using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabConnectedDoctorsService
{
    /// <summary>قائمة الأطباء المتصلين بالمخبر</summary>
    Task<(List<ConnectedDoctorDto>? doctors, string? error)> GetConnectedDoctorsAsync(int labUserId);

    /// <summary>طلبيات طبيب محدد تخص المخبر</summary>
    Task<(List<LabPendingOrderDto>? orders, string? error)> GetOrdersByDentistAsync(
        int labUserId, int dentistId);

    /// <summary>قطع اتصال المخبر مع طبيب</summary>
    Task<string?> DisconnectDoctorAsync(int labUserId, int dentistId);
}
