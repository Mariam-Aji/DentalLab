using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ICalendarService
{
    /// <summary>
    /// يرجع ملخص كل يوم في الشهر (عدد الطلبات + عدد مواعيد المسح)
    /// </summary>
    Task<CalendarMonthDto> GetMonthlyCalendarAsync(int labId, int year, int month);

    /// <summary>
    /// يرجع تفاصيل طلبات ومواعيد مسح يوم محدد
    /// </summary>
    Task<CalendarDayDetailDto> GetDayDetailAsync(int labId, DateOnly date);
}
