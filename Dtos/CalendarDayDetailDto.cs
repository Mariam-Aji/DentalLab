namespace DentalLab.Api.Dtos;

/// <summary>
/// كل تفاصيل الطلب في التقويم اليومي (بدون معلومات المريض)
/// </summary>
public class CalendarOrderDto
{
    public int OrderId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string ImpressionStage { get; set; } = "";
    public string ImpressionType { get; set; } = "";
    public string? Shade { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasAccessories { get; set; }

    public int DentistId { get; set; }
    public string DentistName { get; set; } = "";
    public string DentistEmail { get; set; } = "";
    public string? DentistPhone { get; set; }
    public string? DentistClinicAddress { get; set; }

    public int? LabId { get; set; }

    public List<OrderDetailsItemDto> Items { get; set; } = new();
    public List<string> RequiredImages { get; set; } = new();
    public List<FileDto> Files { get; set; } = new();
}

/// <summary>
/// تفاصيل موعد مسح في التقويم اليومي — يُعرض فقط إذا كان محجوزاً.
/// </summary>
public class CalendarScanVisitDto
{
    public int Id { get; set; }
    public TimeSpan Time { get; set; }
    public string Period { get; set; } = ""; // AM / PM

    public string? DoctorName { get; set; }
    public string? DoctorPhone { get; set; }

    /// <summary>اسم العيادة / المكان</summary>
    public string? DoctorNamePlace { get; set; }

    /// <summary>العنوان التفصيلي للعيادة</summary>
    public string? DoctorAddressPlace { get; set; }

    /// <summary>المدينة</summary>
    public string? DoctorCityPlace { get; set; }

    /// <summary>الدولة</summary>
    public string? DoctorCountryPlace { get; set; }
}

/// <summary>
/// رد التقويم اليومي التفصيلي
/// </summary>
public class CalendarDayDetailDto
{
    public DateOnly Date { get; set; }
    public List<CalendarOrderDto> Orders { get; set; } = new();
    public List<CalendarScanVisitDto> ScanVisits { get; set; } = new();
}
