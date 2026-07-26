using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabInvoiceService
{
    /// <summary>
    /// جلب فواتير الطلبيات والإعلانات المدفوعة للمخبر مرتبة من الأحدث للأقدم
    /// </summary>
    Task<(LabPaidInvoicesResponseDto? result, string? error)> GetPaidInvoicesAsync(int userId);
}
