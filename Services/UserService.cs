using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<(object? Data, string? Error)> SearchUsersServiceAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return (null, "يرجى إدخال كلمة مفتاحية للبحث.");

        // استدعاء الريبو الذكي
        var users = await _userRepo.SearchUsersForAdminAsync(searchTerm);

        if (users == null || users.Count == 0)
        {
            return (new
            {
                TotalResults = 0,
                Message = "لم يتم العثور على أي نتائج مطابقة.",
                CategorizedResults = new Dictionary<string, object>()
            }, null);
        }

        // 🎯 الفلترة والتصنيف التلقائي حسب الـ Role الخاص بكل سجل مسترجع
        var categorizedData = users
            .GroupBy(u => u.Role.ToString())
            .ToDictionary(
                group => group.Key, // الفئة (Dentist, Lab, Admin...)
                group => group.Select(u => new
                {
                    u.Id,
                    Name = u.Name ?? string.Empty,
                    NamePlace = u.NamePlace ?? string.Empty,
                    CityPlace = u.CityPlace ?? string.Empty,
                    CountryPlace = u.CountryPlace ?? string.Empty,
                    Role = u.Role.ToString(),
                    Status = u.Status.ToString()
                }).ToList()
            );

        var response = new
        {
            TotalResults = users.Count,
            SearchQuery = searchTerm,
            CategorizedResults = categorizedData // مجمعة وجاهزة للعرض المقسم
        };

        return (response, null);
    }
    public async Task<(object? Data, string? Error)> GetAllDentistsServiceAsync()
    {
        var doctors = await _userRepo.GetAllDentistsAsync();

        if (doctors == null || doctors.Count == 0)
        {
            return (new
            {
                Count = 0,
                Message = "لا يوجد أطباء مسجلين في النظام حالياً.",
                Doctors = new List<object>()
            }, null);
        }

        // تشكيل البيانات المطلوبة فقط
        var result = new
        {
            Count = doctors.Count,
            Doctors = doctors.Select(d => new
            {
                d.Id,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone ?? string.Empty,
                ClinicName = d.NamePlace ?? string.Empty,       // اسم العيادة
                ClinicAddress = d.AddressPlace ?? string.Empty, // العنوان تفصيلاً
                City = d.CityPlace ?? string.Empty,             // المدينة
                Country = d.CountryPlace ?? string.Empty,       // الدولة
                Status = d.Status.ToString(),                   // حالة الحساب
                d.CreatedAt
            }).ToList()
        };

        return (result, null);
    }
}