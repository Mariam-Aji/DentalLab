using System.ComponentModel.DataAnnotations;
using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos;
//Dto
public class LabPriceUpsertDto
{
    [Required]
    public CompensationType CompensationType { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal UnitPrice { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
