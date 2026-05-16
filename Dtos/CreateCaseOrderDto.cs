using DentalLab.Api.Models;

public class CreateCaseOrderDto
{
    public string Title { get; set; } = string.Empty;
    public string? Shade { get; set; }
    public bool IsTemporary { get; set; }
    public ImpressionType ImpressionType { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public bool HasAccessories { get; set; }
    public List<IFormFile> ImageFiles { get; set; } = new();
}