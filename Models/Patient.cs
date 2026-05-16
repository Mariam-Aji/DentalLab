using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string FullName { get; set; } = null!;
    public int? Age { get; set; }
    public string? ClinicalNotes { get; set; }
    public List<string> ProcessedTeeth { get; set; } = new();

    // Files: before/after photos, digital files, x-rays
    public List<FileResource> Files { get; set; } = new();

    // Navigation
    public List<CaseOrder> CaseOrders { get; set; } = new();
}