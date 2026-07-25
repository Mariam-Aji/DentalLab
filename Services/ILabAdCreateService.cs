using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabAdCreateService
{
    /// <summary>المخبر يقدم طلب إعلان للمراجعة</summary>
    Task<(object? result, string? error)> CreateLabAdvertisementAsync(int labUserId, CreateAdvertisementDto dto);

    /// <summary>إعلانات وافق عليها الأدمن وبانتظار الدفع</summary>
    Task<List<object>> GetPendingPaymentAdsAsync(int labUserId);

    /// <summary>إعلانات نشطة ومدفوعة</summary>
    Task<List<object>> GetActiveAdsAsync(int labUserId);
}
