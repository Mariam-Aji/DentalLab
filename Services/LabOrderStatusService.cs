using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class LabOrderStatusService : ILabOrderStatusService
{
    private readonly ILabOrderStatusRepository _repo;

    public LabOrderStatusService(ILabOrderStatusRepository repo)
    {
        _repo = repo;
    }

    // ----------------------------------------------------------------
    // عدد الطلبات لكل حالة
    // ----------------------------------------------------------------
    public async Task<List<OrderStatusCountDto>> GetOrdersCountByStatusAsync(int labId)
    {
        var counts = await _repo.GetOrdersCountByStatusAsync(labId);

        return counts
            .Select(x => new OrderStatusCountDto
            {
                Status = x.Status.ToString(),
                Count  = x.Count
            })
            .OrderBy(x => x.Status)
            .ToList();
    }

    // ----------------------------------------------------------------
    // الطلبيات بالتفصيل حسب حالة
    // ----------------------------------------------------------------
    public async Task<(List<LabPendingOrderDto>? orders, string? error)> GetOrdersByStatusAsync(
        int labId, CaseStatus status)
    {
        var orders = await _repo.GetOrdersByStatusAsync(labId, status);
        var result = orders.Select(MapToDto).ToList();
        return (result, null);
    }

    // ----------------------------------------------------------------
    // جلب تفاصيل طلبية واحدة خاصة بالمخبر
    // ----------------------------------------------------------------
    public async Task<(LabPendingOrderDto? order, string? error)> GetOrderByIdAsync(
        int orderId, int labId)
    {
        var order = await _repo.GetOrderByIdForLabAsync(orderId, labId);
        if (order == null)
            return (null, "الطلبية غير موجودة أو لا تخص هذا المخبر.");

        return (MapToDto(order), null);
    }

    // ----------------------------------------------------------------
    // تحديث الحالة + رفع صورة النتيجة
    // ----------------------------------------------------------------
    public async Task<(object? result, string? error)> UpdateOrderStatusAsync(
        int orderId, int labId, UpdateOrderStatusDto dto, string uploadsRootPath)
    {
        var order = await _repo.GetOrderByIdAsync(orderId);
        if (order == null)             return (null, "الطلبية غير موجودة.");
        if (order.AssignedLabId != labId) return (null, "ليس لديك صلاحية على هذه الطلبية.");

        // تحديث الحالة
        order.Status = dto.Status;

        // إضافة الملاحظات إن وُجدت
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? dto.Notes
                : order.Notes + "\n" + dto.Notes;
        }

        // رفع صورة النتيجة إن وُجدت
        string? uploadedImagePath = null;

        if (dto.ResultImage != null && dto.ResultImage.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(dto.ResultImage.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
                return (null, "صيغة الصورة غير مدعومة. المسموح: jpg, jpeg, png, webp.");

            var uploadPath = Path.Combine(
                uploadsRootPath, "uploads", "cases", orderId.ToString(), "results");

            Directory.CreateDirectory(uploadPath);

            var fileName      = $"{Guid.NewGuid():N}{ext}";
            var fullFilePath  = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await dto.ResultImage.CopyToAsync(stream);
            }

            uploadedImagePath = $"uploads/cases/{orderId}/results/{fileName}";

            // إضافة مسار الصورة إلى RequiredImages فقط
            order.RequiredImages ??= new List<string>();
            order.RequiredImages.Add(uploadedImagePath);
        }

        await _repo.SaveOrderAsync(order);

        // إعادة تحميل الطلبية من DB لضمان أن قائمة Files محدّثة تشمل الصورة المرفوعة للتو
        var updated = await _repo.GetOrderByIdAsync(orderId);

        return (MapToDto(updated!), null);
    }

    // ----------------------------------------------------------------
    // helper: CaseOrder → LabPendingOrderDto
    // ----------------------------------------------------------------
    private static LabPendingOrderDto MapToDto(CaseOrder co) => new()
    {
        OrderId       = co.Id,
        Title         = co.Title,
        Status        = co.Status.ToString(),
        ImpressionStage = co.ImpressionStage.ToString(),
        ImpressionType  = co.ImpressionType.ToString(),
        Shade         = co.Shade,
        IsTemporary   = co.IsTemporary,
        IsUrgent      = co.IsUrgent,
        DeliveryDate  = co.DeliveryDate,
        Notes         = co.Notes,
        EstimatedPrice = co.EstimatedPrice,
        FinalPrice    = co.FinalPrice,
        IsPaid        = co.IsPaid,
        CreatedAt     = co.CreatedAt,
        HasAccessories = co.HasAccessories,

        DentistId            = co.CreatedById,
        DentistName          = co.CreatedBy?.Name ?? "",
        DentistEmail         = co.CreatedBy?.Email ?? "",
        DentistPhone         = co.CreatedBy?.Phone,
        DentistClinicAddress = co.CreatedBy?.AddressPlace,

        LabId = co.AssignedLabId,

        Items = co.Items.Select(item => new OrderDetailsItemDto
        {
            ItemId           = item.Id,
            CompensationType = item.CompensationType.ToString(),
            ToothNumbers     = item.ToothNumbers
        }).ToList(),

        RequiredImages = co.RequiredImages ?? new List<string>(),

        Files = co.Files.Select(f => new FileDto
        {
            Id         = f.Id,
            Path       = f.Path,
            Type       = f.Type.ToString(),
            UploadedAt = f.UploadedAt
        }).ToList()
    };
}
