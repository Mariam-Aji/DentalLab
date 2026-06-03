using System;
using System.Collections.Generic;

namespace DentalLab.Api.Dtos;

public class CaseOrderDetailDto
{
    public int OrderId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string ImpressionStage { get; set; } = "";
    public string ImpressionType { get; set; } = "";
    public string? Shade { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasAccessories { get; set; }

    public int DentistId { get; set; }
    public string DentistName { get; set; } = null!;
    public string DentistEmail { get; set; } = null!;
    public string? DentistPhone { get; set; }

    public int? LabId { get; set; }
    public string? LabName { get; set; }

    public List<OrderDetailsItemDto> Items { get; set; } = new();
}