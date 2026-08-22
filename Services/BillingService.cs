using DentalLab.Api.DTOs;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public class BillingService : IBillingService
    {
        private readonly ICaseOrderRepository _orderRepo;
        private readonly IAdvertisementRepository _adRepo;

        public BillingService(ICaseOrderRepository orderRepo, IAdvertisementRepository adRepo)
        {
            _orderRepo = orderRepo;
            _adRepo = adRepo;
        }

        public async Task<List<CleanOrderInvoiceDto>> GetPaidOrdersAsync(int dentistId)
        {
            var orders = await _orderRepo.GetPaidOrdersByDentistAsync(dentistId);
            return MapToCleanOrders(orders);
        }

        public async Task<List<CleanOrderInvoiceDto>> GetUnpaidOrdersAsync(int dentistId)
        {
            var orders = await _orderRepo.GetUnpaidOrdersByDentistAsync(dentistId);
            return MapToCleanOrders(orders);
        }

        public async Task<List<CleanAdvertisementInvoiceDto>> GetPaidAdvertisementsAsync(int userId)
        {
            var ads = await _adRepo.GetPaidAdvertisementsByUserAsync(userId);
            return MapToCleanAdvertisements(ads);
        }

        public async Task<List<CleanAdvertisementInvoiceDto>> GetUnpaidAdvertisementsAsync(int userId)
        {
            var ads = await _adRepo.GetUnpaidAdvertisementsByUserAsync(userId);
            return MapToCleanAdvertisements(ads);
        }

        public async Task<(List<CleanOrderInvoiceDto> Orders, List<CleanAdvertisementInvoiceDto> Ads)> GetPaidOrdersAndAdvertisementsAsync(int userId)
        {
            var orders = await _orderRepo.GetPaidOrdersByDentistAsync(userId);
            var ads = await _adRepo.GetPaidAdvertisementsByUserAsync(userId);
            return (MapToCleanOrders(orders), MapToCleanAdvertisements(ads));
        }

        public async Task<(List<CleanOrderInvoiceDto> Orders, List<CleanAdvertisementInvoiceDto> Ads)> GetUnpaidOrdersAndAdvertisementsAsync(int userId)
        {
            var orders = await _orderRepo.GetUnpaidOrdersByDentistAsync(userId);
            var ads = await _adRepo.GetUnpaidAdvertisementsByUserAsync(userId);
            return (MapToCleanOrders(orders), MapToCleanAdvertisements(ads));
        }

        // --- التوابع المضافة حديثاً والمتوافقة مع الكلاس ---

        public async Task<(List<CleanAdvertisementInvoiceDto> Dentists, List<CleanAdvertisementInvoiceDto> Labs, List<CleanAdvertisementInvoiceDto> AdsClients)> GetGroupedPaidAdInvoicesAsync()
        {
            // نستخدم المستودع _adRepo بدلاً من _context
            var paidAds = await _adRepo.GetAllPaidAdvertisementsAsync();

            var dentists = paidAds.Where(a => a.User?.Role == UserRole.Dentist).ToList();
            var labs = paidAds.Where(a => a.User?.Role == UserRole.Lab).ToList();
            var adsClients = paidAds.Where(a => a.User?.Role == UserRole.ADSClient).ToList();

            // نستخدم دالتك القديمة MapToCleanAdvertisements للتحويل
            return (MapToCleanAdvertisements(dentists), MapToCleanAdvertisements(labs), MapToCleanAdvertisements(adsClients));
        }

        public async Task<(List<CleanAdvertisementInvoiceDto> Dentists, List<CleanAdvertisementInvoiceDto> Labs)> GetGroupedUnpaidAdInvoicesAsync()
        {
            // نستخدم المستودع _adRepo بدلاً من _context
            var unpaidAds = await _adRepo.GetAllUnpaidAdvertisementsAsync();

            var dentists = unpaidAds.Where(a => a.User?.Role == UserRole.Dentist).ToList();
            var labs = unpaidAds.Where(a => a.User?.Role == UserRole.Lab).ToList();

            // نستخدم دالتك القديمة MapToCleanAdvertisements للتحويل
            return (MapToCleanAdvertisements(dentists), MapToCleanAdvertisements(labs));
        }


        private List<CleanOrderInvoiceDto> MapToCleanOrders(List<CaseOrder> orders)
        {
            return orders.Select(o => new CleanOrderInvoiceDto
            {
                Id = o.Id,
                Title = o.Title,
                Status = o.Status.ToString(),
                FinalPrice = o.FinalPrice??0,
                IsPaid = o.IsPaid,
                PaidAt = o.PaidAt,
                CreatedAt = o.CreatedAt,
                DentistName = o.CreatedBy?.Name,
                DentistEmail = o.CreatedBy?.Email,
                DentistPhone = o.CreatedBy?.Phone,
                ClinicName = o.CreatedBy?.NamePlace,
                AddressPlace = o.CreatedBy?.AddressPlace,
                CityPlace = o.CreatedBy?.CityPlace,
                CountryPlace = o.CreatedBy?.CountryPlace,

                // 👈 قراءة الاسم الحقيقي للمخبر من NamePlace (أو Name كبديل) والوصف من Description
                LabName = o.AssignedLab?.Owner?.NamePlace ?? o.AssignedLab?.Owner?.Name ?? "غير محدد",
                LabDescription = o.AssignedLab?.Description,

                Items = o.Items.Select(i => i.CompensationType.ToString()).ToList()
            }).ToList();
        }

        // دالة تحويل الإعلانات لإخفاء كلمات المرور والحقول الزائدة
        private List<CleanAdvertisementInvoiceDto> MapToCleanAdvertisements(List<Advertisement> ads)
        {
            return ads.Select(a => new CleanAdvertisementInvoiceDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                ImageUrl = a.ImageUrl,
                Target = a.Target.ToString(),
                CreatedAt = a.CreatedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive,
                Price = a.Price,
                IsPaid = a.IsPaid,
                PaidAt = a.PaidAt,
                UserName = a.User?.Name ?? "",
                UserEmail = a.User?.Email ?? "",
                UserPhone = a.User?.Phone ?? "",
                NamePlace = a.User?.NamePlace ?? "",
                AddressPlace = a.User?.AddressPlace ?? "",
                CityPlace = a.User?.CityPlace ?? "",
                CountryPlace = a.User?.CountryPlace ?? ""
            }).ToList();
        }
    }
}




