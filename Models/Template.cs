using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum CompensationType { Veneer, ZirconCrown, ImplantCrown, Bridge, FullDenture, PartialDenture, Other }
public enum ImpressionType { Traditional, Digital }

public class Template
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public CompensationType Compensation { get; set; }
    public string? Material { get; set; }
    public string? DefaultShade { get; set; }
    public ImpressionType DefaultImpression { get; set; } = ImpressionType.Digital;
    public List<string> RequiredPhotos { get; set; } = new();
    public int DefaultTurnaroundDays { get; set; } = 5;
    public string? Notes { get; set; }
}