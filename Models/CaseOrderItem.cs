using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public class CaseOrderItem
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int CaseOrderId { get; set; }
    public CaseOrder? CaseOrder { get; set; }
    [Required] 
    public CompensationType CompensationType { get; set; }
    public List<int> ToothNumbers { get; set; } = new();
}
