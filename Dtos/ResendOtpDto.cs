using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class ResendOtpDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = null!;
}
