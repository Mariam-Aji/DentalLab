using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabDentistProfileService
{
    /// <summary>
    /// جلب تفاصيل طبيب محدد بالـ dentistId — متاح للمخبر
    /// </summary>
    Task<(DentistProfileDto? result, string? error)> GetDentistProfileAsync(int dentistId);
}
