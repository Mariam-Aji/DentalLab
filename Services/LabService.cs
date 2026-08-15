using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.DTOs;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services
{
    public class LabService : ILabService
    {
        private readonly ILabRepository _labRepository;

        public LabService(ILabRepository labRepository)
        {
            _labRepository = labRepository;
        }

        public async Task<IEnumerable<LabDto>> GetLabsSummaryAsync(int? currentDentistId = null)
        {
            var labs = await _labRepository.GetAllLabsWithOwnersAsync();
            return await MapToDtoListAsync(labs, currentDentistId);
        }

        public async Task<IEnumerable<LabDto>> GetConnectedLabsAsync(int? currentDentistId = null)
        {
            if (!currentDentistId.HasValue)
            {
                return Enumerable.Empty<LabDto>();
            }

            var connectedLabs = await _labRepository.GetConnectedLabsForDentistAsync(currentDentistId.Value);

            return connectedLabs.Select(l => CalculateLabDto(l, true)).ToList();
        }

        public async Task<IEnumerable<LabDto>> GetDisconnectedLabsAsync(int? currentDentistId = null)
        {
            var allLabs = await _labRepository.GetAllLabsWithOwnersAsync();

            if (!currentDentistId.HasValue)
            {
                return await MapToDtoListAsync(allLabs, null);
            }

            var connectedLabIds = await _labRepository.GetConnectedLabIdsForDentistAsync(currentDentistId.Value);
            var disconnectedLabs = allLabs.Where(l => !connectedLabIds.Contains(l.Id));

            return await MapToDtoListAsync(disconnectedLabs, currentDentistId);
        }

        private async Task<IEnumerable<LabDto>> MapToDtoListAsync(IEnumerable<Lab> labs, int? currentDentistId)
        {
            List<int> connectedLabIds = new();

            if (currentDentistId.HasValue)
            {
                connectedLabIds = await _labRepository.GetConnectedLabIdsForDentistAsync(currentDentistId.Value);
            }

            return labs.Select(l => CalculateLabDto(l, currentDentistId.HasValue && connectedLabIds.Contains(l.Id))).ToList();
        }

        // تابع مساعد حاسم وموحّد لحساب التقييمات وبناء الـ DTO لمنع التكرار والأخطاء
        private static LabDto CalculateLabDto(Lab lab, bool isConnected)
        {
            double average = 0.0;
            int count = 0;

            if (lab.Ratings != null && lab.Ratings.Any())
            {
                var validRatings = lab.Ratings.Where(r => r.Overall > 0).ToList();
                count = lab.Ratings.Count;

                if (validRatings.Any())
                {
                    average = validRatings.Average(r => r.Overall);
                }
            }

            // إذا لم تتوفر تقييمات ديناميكية في الجدول المربوط، نأخذ الحقل الثابت إذا كان يحتوي قيمة
            if (average == 0.0 && lab.AverageRating > 0)
            {
                average = lab.AverageRating;
            }

            return new LabDto
            {
                Id = lab.Id,
                Name = lab.Owner != null ? (lab.Owner.NamePlace ?? lab.Owner.Name) : "Unknown Lab Name",
                ProfilePictureUrl = lab.Owner?.ProfilePictureUrl,

                // الإضافات الجديدة: رقم الهاتف وموقع المخبر من جدول الـ User
                Phone = lab.Owner?.Phone,
                AddressPlace = lab.Owner?.AddressPlace,
                CityPlace = lab.Owner?.CityPlace,
                CountryPlace = lab.Owner?.CountryPlace,

                IsConnected = isConnected,
                AverageRating = Math.Round(average, 1),
                RatingsCount = count
            };
        }
        public async Task<List<AdminLabDto>> GetAllLabsForAdminAsync()
        {
            // جلب المخابر مع بيانات المالك باستخدام الـ Repository بدلاً من الـ DbContext المباشر
            var labs = await _labRepository.GetAllLabsWithOwnersAsync();

            return labs.Select(l => new AdminLabDto
            {
                Id = l.Id,
                Description = l.Description,
                YearsOfExperience = l.YearsOfExperience,
                Specialties = l.Specialties ?? new List<string>(),
                Materials = l.Materials ?? new List<string>(),
                Availability = l.Availability.ToString(), // تحويل الـ Enum إلى نص مباشر
                HasScanVisitService = l.HasScanVisitService,
                AverageRating = l.AverageRating,
                SubscriptionStartUtc = l.SubscriptionStartUtc,
                SubscriptionEndUtc = l.SubscriptionEndUtc,

                // تعيين بيانات صاحب المخبر بأمان
                OwnerId = l.Owner?.Id ?? 0,
                OwnerName = l.Owner?.Name ?? "",
                OwnerEmail = l.Owner?.Email ?? "",
                OwnerPhone = l.Owner?.Phone ?? "",
                LabNamePlace = l.Owner?.NamePlace ?? "",
                AddressPlace = l.Owner?.AddressPlace ?? "",
                CityPlace = l.Owner?.CityPlace ?? "",
                CountryPlace = l.Owner?.CountryPlace ?? "",
                ProfilePictureUrl = l.Owner?.ProfilePictureUrl
            }).ToList();
        }
        public async Task<int?> GetLabIdByUserIdAsync(int userId)
        {
            return await _labRepository.GetLabIdByUserIdAsync(userId);
        }
    }
}