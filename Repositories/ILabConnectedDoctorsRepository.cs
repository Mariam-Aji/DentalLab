using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabConnectedDoctorsRepository
{
    /// <summary>جلب الأطباء المتصلين بمخبر معين (accepted connections)</summary>
    Task<List<User>> GetConnectedDoctorsAsync(int labId);

    /// <summary>جلب طلبيات طبيب محدد لمخبر محدد</summary>
    Task<List<CaseOrder>> GetOrdersByDentistForLabAsync(int labId, int dentistId);

    /// <summary>جلب طلب الاتصال المقبول بين طبيب ومخبر</summary>
    Task<ConnectionRequest?> GetAcceptedConnectionAsync(int labId, int dentistId);

    /// <summary>حذف طلب الاتصال</summary>
    Task DeleteConnectionAsync(ConnectionRequest connection);
}
