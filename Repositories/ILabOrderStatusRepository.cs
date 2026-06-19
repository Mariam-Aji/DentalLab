using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabOrderStatusRepository
{
    /// <summary>عدد الطلبات لكل حالة خاصة بمخبر معين</summary>
    Task<List<(CaseStatus Status, int Count)>> GetOrdersCountByStatusAsync(int labId);

    /// <summary>جلب الطلبيات بالتفصيل حسب حالة معينة</summary>
    Task<List<CaseOrder>> GetOrdersByStatusAsync(int labId, CaseStatus status);

    /// <summary>جلب طلبية واحدة مع ملفاتها للتحقق من الصلاحية</summary>
    Task<CaseOrder?> GetOrderByIdAsync(int orderId);

    /// <summary>جلب طلبية واحدة تخص مخبراً محدداً</summary>
    Task<CaseOrder?> GetOrderByIdForLabAsync(int orderId, int labId);

    /// <summary>حفظ التغييرات على الطلبية</summary>
    Task SaveOrderAsync(CaseOrder order);

    /// <summary>إضافة ملف نتيجة مرتبط بالطلبية</summary>
    Task AddFileAsync(FileResource file);
}
