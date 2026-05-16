using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum ScanVisitStatus { Pending, Accepted, Rejected, Completed }

public class ScanVisitRequest
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    [Required]
    public int DentistId { get; set; }
    public User? Dentist { get; set; }
    public DateTime ScheduledAt { get; set; }
    public ScanVisitStatus Status { get; set; } = ScanVisitStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}