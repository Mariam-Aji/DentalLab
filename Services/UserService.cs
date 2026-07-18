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
    private readonly IWebHostEnvironment _env;
    public UserService(IUserRepository userRepo, IWebHostEnvironment env)
    {
        _userRepo = userRepo;
        _env = env;
    }

    public async Task<(object? Data, string? Error)> SearchUsersServiceAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return (null, "يرجى إدخال كلمة مفتاحية للبحث.");

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

        var categorizedData = users
            .GroupBy(u => u.Role.ToString())
            .ToDictionary(
                group => group.Key, 
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
            CategorizedResults = categorizedData 
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
    public async Task<(string? RelativePath, string? Error)> UpdateProfilePictureAsync(int userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (null, "الملف غير صالح.");

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return (null, "حجم الصورة كبير جداً، الحد الأقصى المسموح به هو 5 ميغابايت.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(ext))
            return (null, "صيغة الصورة غير مدعومة. الصيغ المسموحة هي: JPG, JPEG, PNG.");

        var user = await _userRepo.GetUserByIdAsync(userId);
        if (user == null)
            return (null, "المستخدم غير موجود في النظام.");

        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "dentists", userId.ToString(), "profile");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        var relativePath = Path.Combine("uploads", "dentists", userId.ToString(), "profile", fileName).Replace("\\", "/");

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            var oldFullPath = Path.Combine(_env.ContentRootPath, user.ProfilePictureUrl.Replace("/", "\\"));
            if (File.Exists(oldFullPath))
            {
                try { File.Delete(oldFullPath); } catch { }
            }
        }

        user.ProfilePictureUrl = relativePath;
        var success = await _userRepo.UpdateUserAsync(user);

        if (!success)
            return (null, "فشل في حفظ مسار الصورة ضمن قاعدة البيانات.");

        return (relativePath, null);
    }

}