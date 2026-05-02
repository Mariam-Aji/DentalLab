using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = null!;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = null!;
}
