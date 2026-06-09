using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }
   
    public string FullName { get; set; } = null!;
    public int? Age { get; set; }
    public string? ClinicalNotes { get; set; }
    public List<string> ProcessedTeeth { get; set; } = new();

    public List<FileResource> Files { get; set; } = new();

    public List<CaseOrder> CaseOrders { get; set; } = new();
}