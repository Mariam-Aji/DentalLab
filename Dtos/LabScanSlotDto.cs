namespace DentalLab.Api.Dtos;

/// <summary>
/// DTO لإضافة أو تعديل موعد مسح
/// </summary>
public class UpsertScanSlotDto
{
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Period { get; set; } = "AM"; // AM أو PM
}

/// <summary>
/// DTO لعرض موعد مسح للمخبر
/// </summary>
public class LabScanSlotResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string TimeFormatted => DateTime.Today.Add(Time).ToString("hh:mm") + " " + Period;
    public string Period { get; set; } = "";
    public bool IsBooked { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO لعرض الحجوزات (المواعيد المحجوزة من قِبَل الدكاترة)
/// </summary>
public class ScanVisitBookingDto
{
    public int BookingId { get; set; }
    public int SlotId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string TimeFormatted => DateTime.Today.Add(Time).ToString("hh:mm") + " " + Period;
    public string Period { get; set; } = "";
    public int DentistId { get; set; }
    public string DentistName { get; set; } = "";
    public string DentistEmail { get; set; } = "";
    public string? DentistPhone { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? ClinicCity { get; set; }
    public string? ClinicCountry { get; set; }
    public DateTime BookedAt { get; set; }
}
