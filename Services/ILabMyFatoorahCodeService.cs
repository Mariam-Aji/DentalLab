using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabMyFatoorahCodeService
{
    /// <summary>
    /// جلب كود حساب MyFatoorah للمخبر
    /// </summary>
    Task<(LabMyFatoorahCodeResponseDto? result, string? error)> GetCodeAsync(int userId);

    /// <summary>
    /// تعديل أو حذف كود حساب MyFatoorah للمخبر
    /// </summary>
    Task<(LabMyFatoorahCodeResponseDto? result, string? error)> UpdateCodeAsync(int userId, UpdateLabMyFatoorahCodeDto dto);
}
