namespace DentalLab.Api.Dtos;

public class CreatePatientRequestDto
{
    public string FullName { get; set; } = null!;
    public int? Age { get; set; }
    public string? ClinicalNotes { get; set; }
    public List<string> ProcessedTeeth { get; set; } = new();
    //
}