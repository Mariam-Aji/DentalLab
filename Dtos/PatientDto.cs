namespace DentalLab.Api.Dtos;

public class PatientDto
{
    public string FullName { get; set; } = null!;
    public int? Age { get; set; }
    public string? ClinicalNotes { get; set; }
    public List<string> ProcessedTeeth { get; set; } = new();
    public List<int> FileIds { get; set; } = new();
//
}