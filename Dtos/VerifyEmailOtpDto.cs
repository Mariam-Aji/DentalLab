using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class VerifyEmailOtpDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = null!;
}
