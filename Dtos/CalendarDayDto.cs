namespace DentalLab.Api.Dtos;

/// <summary>
/// ملخص يوم واحد في التقويم الشهري
/// </summary>
public class CalendarDayDto
{
    public DateOnly Date { get; set; }
    public int OrdersCount { get; set; }
    public int ScanVisitsCount { get; set; }
}

/// <summary>
/// رد التقويم الشهري
/// </summary>
public class CalendarMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<CalendarDayDto> Days { get; set; } = new();
}
