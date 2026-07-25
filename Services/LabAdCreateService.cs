using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabAdCreateService : ILabAdCreateService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public LabAdCreateService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<(object? result, string? error)> CreateLabAdvertisementAsync(int labUserId, CreateAdvertisementDto dto)
    {        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == labUserId);
        if (user == null) return (null, "المستخدم غير موجود.");

        // رفع الصور
        var uploadedPaths = new List<string>();
        if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var folder = Path.Combine(_env.ContentRootPath, "uploads", "advertisements", labUserId.ToString());
            Directory.CreateDirectory(folder);

            foreach (var file in dto.ImageFiles)
            {
                if (file.Length == 0) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext)) return (null, $"الامتداد {ext} غير مسموح به.");

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, fileName);
                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);
                uploadedPaths.Add($"uploads/advertisements/{labUserId}/{fileName}");
            }
        }

        var ad = new Advertisement
        {
            Title     = dto.Target switch
            {
                TargetAudience.Dentists => "إعلان موجه لأطباء الأسنان",
                TargetAudience.Labs     => "إعلان موجه لمخابر الأسنان",
                TargetAudience.Both     => "إعلان عام للأطباء والمخابر",
                _                       => "إعلان موجه لأطباء الأسنان"
            },
            Content   = dto.Content ?? "",
            Target    = dto.Target,
            ImageUrl  = uploadedPaths.Count > 0 ? string.Join(";", uploadedPaths) : null,
            ExpiresAt = dto.ExpiresAt,
            UserId    = labUserId,
            IsActive  = false,
            IsPaid    = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Advertisements.Add(ad);
        await _context.SaveChangesAsync();

        // إشعار الأدمن
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        if (admin != null)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                message = "تم تقديم طلب إعلان جديد من مخبري بانتظار مراجعتك.",
                advertisement = new
                {
                    id        = ad.Id,
                    title     = ad.Title,
                    content   = ad.Content,
                    userId    = ad.UserId,
                    isActive  = ad.IsActive,
                    createdAt = ad.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    images    = uploadedPaths
                }
            }, new System.Text.Json.JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            });

            _context.Notifications.Add(new Notification
            {
                RecipientId = admin.Id,
                Message     = payload,
                Type        = NotificationType.StatusChanged,
                IsRead      = false,
                CreatedAt   = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return (new
        {
            message = "تم تقديم طلب الإعلان بنجاح، وتم إرساله للأدمن للمراجعة والتفعيل.",
            advertisement = new
            {
                ad.Id,
                ad.Title,
                ad.Content,
                ad.UserId,
                ad.IsActive,
                ad.IsPaid,
                ad.CreatedAt,
                ad.ExpiresAt,
                Images = uploadedPaths
            }
        }, null);
    }

    // ----------------------------------------------------------------
    // وافق عليها الأدمن وبانتظار الدفع: IsActive=false, IsPaid=false, Price > 0
    // ----------------------------------------------------------------
    public async Task<List<object>> GetPendingPaymentAdsAsync(int labUserId)
    {
        var ads = await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.UserId == labUserId && !a.IsActive && !a.IsPaid && a.Price != null && a.Price > 0)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return ads.Select(MapAd).ToList();
    }

    // ----------------------------------------------------------------
    // نشطة ومدفوعة: IsActive=true, IsPaid=true
    // ----------------------------------------------------------------
    public async Task<List<object>> GetActiveAdsAsync(int labUserId)
    {
        var now = DateTime.UtcNow;
        var ads = await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.UserId == labUserId && a.IsActive && a.IsPaid &&
                        (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return ads.Select(MapAd).ToList();
    }

    // ---- helper ----
    private static object MapAd(Advertisement a) => new
    {
        a.Id,
        a.Title,
        a.Content,
        Target    = a.Target.ToString(),
        a.Price,
        a.IsActive,
        a.IsPaid,
        a.CreatedAt,
        a.ExpiresAt,
        Images = string.IsNullOrEmpty(a.ImageUrl)
            ? new List<string>()
            : a.ImageUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
    };
}
