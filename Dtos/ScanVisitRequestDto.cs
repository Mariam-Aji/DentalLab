namespace DentalLab.Api.DTOs;

public class ScanVisitRequestDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public string? LabName { get; set; }
    public int LabScanSlotId { get; set; }
    public DateTime CreatedAt { get; set; }
}