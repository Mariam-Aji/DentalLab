namespace DentalLab.Api.DTOs;

public class DentistScanVisitDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public string? LabName { get; set; }
    public string? LabAddress { get; set; }
    public string? LabPhone { get; set; }

    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public string Period { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}