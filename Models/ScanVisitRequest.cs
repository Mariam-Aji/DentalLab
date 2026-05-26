using DentalLab.Api.Models;
using System.ComponentModel.DataAnnotations;

public class ScanVisitRequest
{
    [Key]
    public int Id { get; set; }

    public int LabId { get; set; }
    public Lab? Lab { get; set; }

    public int DentistId { get; set; }
    public User? Dentist { get; set; }

    public int LabScanSlotId { get; set; }
    public LabScanSlot? Slot { get; set; }

    //public ScanVisitStatus Status { get; set; } = ScanVisitStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}