using DentalLab.Api.DTOs;

namespace DentalLab.Api.Services
{
    public interface IBillingService
    {
        Task<List<CleanOrderInvoiceDto>> GetPaidOrdersAsync(int dentistId);
        Task<List<CleanOrderInvoiceDto>> GetUnpaidOrdersAsync(int dentistId);

        Task<List<CleanAdvertisementInvoiceDto>> GetPaidAdvertisementsAsync(int userId);
        Task<List<CleanAdvertisementInvoiceDto>> GetUnpaidAdvertisementsAsync(int userId);

        Task<(List<CleanOrderInvoiceDto> Orders, List<CleanAdvertisementInvoiceDto> Ads)> GetPaidOrdersAndAdvertisementsAsync(int userId);
        Task<(List<CleanOrderInvoiceDto> Orders, List<CleanAdvertisementInvoiceDto> Ads)> GetUnpaidOrdersAndAdvertisementsAsync(int userId);
        Task<(List<CleanAdvertisementInvoiceDto> Dentists, List<CleanAdvertisementInvoiceDto> Labs, List<CleanAdvertisementInvoiceDto> AdsClients)> GetGroupedPaidAdInvoicesAsync();

        Task<(List<CleanAdvertisementInvoiceDto> Dentists, List<CleanAdvertisementInvoiceDto> Labs)> GetGroupedUnpaidAdInvoicesAsync();
    }
}