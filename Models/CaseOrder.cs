using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum CaseStatus
{
    Pennding,
    Accepted,
    RequestInfo,
    InDesign,
    InProduction,
    WaitingForClarification,
    Ready,
    Delivered,
    Cancelled
}
public enum ImpressionStage
{
    PlasticImpression,
    FinalImpression
}

public class CaseOrder
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public int? AssignedLabId { get; set; }
    public Lab? AssignedLab { get; set; }

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public CaseStatus Status { get; set; }
        = CaseStatus.Pennding;

    public ImpressionStage ImpressionStage { get; set; }
        = ImpressionStage.PlasticImpression;

    public List<CaseOrderItem> Items { get; set; } = new();

    public string? Shade { get; set; }

    public bool IsTemporary { get; set; }

    public ImpressionType ImpressionType { get; set; }
        = ImpressionType.Digital;

    public bool IsUrgent { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string? Notes { get; set; }

    public List<FileResource> Files { get; set; } = new();

    public bool HasAccessories { get; set; }

    public List<string> RequiredImages { get; set; } = new();

    public decimal? EstimatedPrice { get; set; }

    public decimal? FinalPrice { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;
    public OrderInvoice? Invoice { get; set; }
}