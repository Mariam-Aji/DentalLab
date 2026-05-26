using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public class LabPrice
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    [Required]
    public CompensationType CompensationType { get; set; }
    [Required]
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
