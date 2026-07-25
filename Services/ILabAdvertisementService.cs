using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabAdvertisementService
{
    /// <summary>
    /// يرجع الإعلانات النشطة والمدفوعة المخصصة للمخابر (Labs أو Both)
    /// </summary>
    Task<List<LabAdvertisementDto>> GetAdsForLabAsync();
}
