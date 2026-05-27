using DentalLab.Api.Models;
using Microsoft.AspNetCore.Http;

public class CreateCaseOrderDto
{
    public string Title { get; set; } = string.Empty;

    public string? Shade { get; set; }

    public bool IsTemporary { get; set; }

    public ImpressionType ImpressionType { get; set; }

  
    public ImpressionStage ImpressionStage { get; set; }

    public bool IsUrgent { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string? Notes { get; set; }

    public bool HasAccessories { get; set; }

    public List<IFormFile> RequiredImages { get; set; } = new();
    //
}