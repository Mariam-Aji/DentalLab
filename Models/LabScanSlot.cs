using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum SlotPeriod
{
    AM,
    PM
}

public class LabScanSlot
{
    [Key]
    public int Id { get; set; }

    public int LabId { get; set; }
    public Lab? Lab { get; set; }

    public DateTime Date { get; set; }   

    public TimeSpan Time { get; set; }  

    public SlotPeriod Period { get; set; } // AM / PM

    public bool IsBooked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ScanVisitRequest? Booking { get; set; }
}