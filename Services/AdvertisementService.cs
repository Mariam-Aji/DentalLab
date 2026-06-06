using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public class AdvertisementService : IAdvertisementService
{
    private readonly IAdvertisementRepository _advRepo;
    private readonly IWebHostEnvironment _env;

    public AdvertisementService(IAdvertisementRepository advRepo, IWebHostEnvironment env)
    {
        _advRepo = advRepo;
        _env = env;
    }

    public async Task<(User? result, string? error)> CreateADSClientByAdminAsync(CreateADSClientDto dto)
    {
        string generatedEmail = $"ads.{dto.Name.Replace(" ", "").ToLower()}.{DateTime.UtcNow.Ticks}@dentallab.local";
        string defaultPasswordHash = "NO_PASSWORD_ASSIGNED";

        var adsClient = new User
        {
            Name = dto.Name,
            Email = generatedEmail,
            PasswordHash = defaultPasswordHash,
            Phone = dto.Phone,
            NamePlace = dto.NamePlace,
            AddressPlace = dto.AddressPlace,
            CityPlace = dto.CityPlace,
            CountryPlace = dto.CountryPlace,
            Role = UserRole.ADSClient,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var savedUser = await _advRepo.SaveUserAsync(adsClient);
        return (savedUser, null);
    }

    public async Task<(Advertisement? result, string? error)> CreateAdvertisementAsync(int userId, CreateAdvertisementDto dto)
    {
        if (!await _advRepo.IsUserExistsAsync(userId))
        {
            return (null, "المستخدم الممرر غير موجود في النظام.");
        }

        try
        {
            string fixedTitle = dto.Target switch
            {
                TargetAudience.Dentists => "إعلان موجه لأطباء الأسنان",
                TargetAudience.Labs => "إعلان موجه لمخابر الأسنان",
                TargetAudience.Both => "إعلان عام للأطباء والمخابر",
                _ => "إعلان جديد"
            };

            var uploadedImagePaths = new List<string>();

            if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var advUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "advertisements", userId.ToString());

                if (!Directory.Exists(advUploadsFolder)) Directory.CreateDirectory(advUploadsFolder);

                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(ext))
                            return (null, $"الامتداد {ext} غير مسموح به.");

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var fullPath = Path.Combine(advUploadsFolder, fileName);

                        await using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedImagePaths.Add($"uploads/advertisements/{userId}/{fileName}");
                    }
                }
            }

            string? finalImagesString = uploadedImagePaths.Count > 0 ? string.Join(";", uploadedImagePaths) : null;

            var advertisement = new Advertisement
            {
                Title = fixedTitle,
                Target = dto.Target,
                Content = dto.Content,
                ImageUrl = finalImagesString,
                ExpiresAt = dto.ExpiresAt,
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var savedAdv = await _advRepo.SaveAdvertisementAsync(advertisement);
            return (savedAdv, null);
        }
        catch (Exception ex)
        {
            return (null, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
        }
    }

    public async Task<List<Advertisement>> GetAllAdvertisementsForAdminAsync()
    {
        return await _advRepo.GetAllAdvertisementsAsync();
    }

    public async Task<(Advertisement? result, string? error)> UpdateAdvertisementAsync(int advId, UpdateAdvertisementDto dto)
    {
        var adv = await _advRepo.GetAdvertisementByIdAsync(advId);
        if (adv == null) return (null, "الإعلان غير موجود في النظام.");

        try
        {
            if (dto.Target != null)
            {
                adv.Title = dto.Target switch
                {
                    TargetAudience.Dentists => "إعلان موجه لأطباء الأسنان",
                    TargetAudience.Labs => "إعلان موجه لمخابر الأسنان",
                    TargetAudience.Both => "إعلان عام للأطباء والمخابر",
                    _ => adv.Title
                };
            }
            else if (!string.IsNullOrEmpty(dto.Title))
            {
                adv.Title = dto.Title;
            }

            if (!string.IsNullOrEmpty(dto.Content))
            {
                adv.Content = dto.Content;
            }

            if (dto.ExpiresAt.HasValue)
            {
                adv.ExpiresAt = dto.ExpiresAt.Value;
            }

            if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
            {
                if (!string.IsNullOrEmpty(adv.ImageUrl))
                {
                    var oldPaths = adv.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var oldPath in oldPaths)
                    {
                        var fullOldPath = Path.Combine(_env.ContentRootPath, oldPath);
                        if (File.Exists(fullOldPath))
                        {
                            File.Delete(fullOldPath);
                        }
                    }
                }

                var newImagePaths = new List<string>();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string userFolderId = adv.UserId.ToString();
                var advUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "advertisements", userFolderId);

                if (!Directory.Exists(advUploadsFolder))
                {
                    Directory.CreateDirectory(advUploadsFolder);
                }

                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(ext))
                            return (null, $"الامتداد {ext} غير مسموح به.");

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var fullPath = Path.Combine(advUploadsFolder, fileName);

                        await using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        newImagePaths.Add($"uploads/advertisements/{userFolderId}/{fileName}");
                    }
                }

                adv.ImageUrl = string.Join(";", newImagePaths);
            }

            var isUpdated = await _advRepo.UpdateAdvertisementAsync(adv);
            if (!isUpdated) return (null, "فشل تحديث البيانات أو لم يتم تعديل أي حقول جديدة.");

            return (adv, null);
        }
        catch (Exception ex)
        {
            return (null, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
        }
    }

    public async Task<(bool success, string? error)> DeleteAdvertisementAsync(int advId)
    {
        var adv = await _advRepo.GetAdvertisementByIdAsync(advId);
        if (adv == null) return (false, "الإعلان غير موجود.");

        try
        {
            if (!string.IsNullOrEmpty(adv.ImageUrl))
            {
                var paths = adv.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in paths)
                {
                    var fullPath = Path.Combine(_env.ContentRootPath, path);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
            }

            var isDeleted = await _advRepo.DeleteAdvertisementAsync(adv);
            return (isDeleted, isDeleted ? null : "فشل حذف الإعلان من قاعدة البيانات.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(Advertisement? result, string? error)> ToggleAdStatusAsync(int advId)
    {
        var adv = await _advRepo.GetAdvertisementByIdAsync(advId);
        if (adv == null) return (null, "الإعلان غير موجود.");

        adv.IsActive = !adv.IsActive;

        var isUpdated = await _advRepo.UpdateAdvertisementAsync(adv);
        return isUpdated ? (adv, null) : (null, "فشل تحديث حالة التفعيل.");
    }

    public async Task<List<Advertisement>> GetAdvertisementsForDentistsAsync()
    {
        return await _advRepo.GetAdvertisementsForDentistsAsync();
    }

    public async Task<List<Advertisement>> GetAdvertisementsForLabsAsync()
    {
        return await _advRepo.GetAdvertisementsForLabsAsync();
    }

    public async Task<(Advertisement? result, string? error)> CreateAdvertisementByDoctorAsync(int doctorId, CreateAdvertisementDto dto)
    {
        if (!await _advRepo.IsUserExistsAsync(doctorId))
        {
            return (null, "حساب الطبيب الممرر غير موجود.");
        }

        try
        {
            TargetAudience fixedTarget = TargetAudience.Dentists;
            string fixedTitle = "إعلان موجه لأطباء الأسنان";

            var uploadedImagePaths = new List<string>();
            if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var advUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "advertisements", doctorId.ToString());

                if (!Directory.Exists(advUploadsFolder)) Directory.CreateDirectory(advUploadsFolder);

                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(ext))
                            return (null, $"الامتداد {ext} غير مسموح به.");

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var fullPath = Path.Combine(advUploadsFolder, fileName);

                        await using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedImagePaths.Add($"uploads/advertisements/{doctorId}/{fileName}");
                    }
                }
            }

            string? finalImagesString = uploadedImagePaths.Count > 0 ? string.Join(";", uploadedImagePaths) : null;

            var advertisement = new Advertisement
            {
                Title = fixedTitle,
                Target = fixedTarget,
                Content = dto.Content,
                ImageUrl = finalImagesString,
                ExpiresAt = dto.ExpiresAt,
                UserId = doctorId,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            var savedAdv = await _advRepo.SaveAdvertisementAsync(advertisement);

            var admin = await _advRepo.GetAdminUserAsync();
            if (admin != null)
            {
                var responseImages = !string.IsNullOrEmpty(savedAdv.ImageUrl)
                    ? savedAdv.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>();

                // بناء هيكلية الـ JSON بالكامل لتطابق شكل استجابة الطلب تماماً
               var notificationPayload = new
{
    message = "تم تقديم طلب الإعلان بنجاح، وتم إرسال محتواه بالكامل إلى الأدمن للمراجعة والتفعيل.",
    advertisement = new
    {
        id = savedAdv.Id,
        title = savedAdv.Title,
        content = savedAdv.Content,
        userId = savedAdv.UserId,
        isActive = savedAdv.IsActive,
        createdAt = savedAdv.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
        // التعديل هنا: التعامل مع الـ Nullable DateTime بأمان
        expiresAt = savedAdv.ExpiresAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
        images = responseImages
    }
};

// الحفاظ على تشفير الحروف والكلمات العربية لتخزينها بشكل مقروء
var jsonOptions = new JsonSerializerOptions
{
    // التعديل هنا: استخدام الـ Namespace الصحيح للـ Encoder
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
    WriteIndented = true
};

                string jsonMessage = JsonSerializer.Serialize(notificationPayload, jsonOptions);

                var notification = new Notification
                {
                    RecipientId = admin.Id,
                    Message = jsonMessage, // إسناد نص الـ JSON الكامل كرسالة
                    Type = NotificationType.StatusChanged,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _advRepo.SaveNotificationAsync(notification);
            }

            return (savedAdv, null);
        }
        catch (Exception ex)
        {
            return (null, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
        }
    }

    public async Task<(bool isActivated, string? errorMessage)> ActivateDoctorAdvertisementAsync(int advertisementId, int userId, decimal price)
    {
        var advertisement = await _advRepo.GetByIdAsync(advertisementId);
        if (advertisement == null)
        {
            return (false, "الإعلان المطلوب غير موجود.");
        }

        // التحقق من توافق الإعلان مع معرف الطبيب الممرر بالـ Route لضمان أمان البيانات
        if (advertisement.UserId != userId)
        {
            return (false, "هذا الإعلان لا ينتمي للطبيب الممرر المعرف الخاص به.");
        }

        if (advertisement.IsActive)
        {
            return (false, "هذا الإعلان نشط بالفعل ومقبول سابقاً في النظام.");
        }

        // 🎯 التعديل الجديد: تخزين السعر المحتسب والمحدد من قبل الأدمن في حقل الـ Price المضاف حديثاً
        advertisement.Price = price;

        // يبقى غير منشور بناءً على طلبكِ إلى حين الدفع الفعلي من قبل الطبيب
        advertisement.IsActive = false;
        advertisement.CreatedAt = DateTime.UtcNow;

        // حفظ التعديلات الجديدة (بما فيها السعر وحالة النشاط ووقت التحديث) في قاعدة البيانات
        var isSaved = await _advRepo.SaveChangesStatusAsync(advertisement);
        if (!isSaved)
        {
            return (false, "فشلت عملية تحديث بيانات الإعلان وحفظ السعر في قاعدة البيانات.");
        }

        try
        {
            // إنشاء وإرسال إشعار القبول المشروط بالقيمة المالية للطبيب
            var doctorNotification = new Notification
            {
                RecipientId = userId, // معرف الطبيب المستلم
                Message = $"✅ تمت الموافقة على محتوى إعلانك بعنوان: '{advertisement.Title}'. يرجى سداد مبلغ ({price}) لتفعيل الإعلان ونشره رسمياً داخل التطبيق. علمًا أن الإعلان سيبقى غير منشور حتى إتمام الدفع.",
                Type = NotificationType.StatusChanged,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _advRepo.SaveNotificationAsync(doctorNotification);
        }
        catch
        {
            // وضعها داخل catch فارغة لضمان عدم توقف العملية الأساسية في حال وجود مشكلة عابرة بجدول الإشعارات
        }

        return (true, null);
    }
}