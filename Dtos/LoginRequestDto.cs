using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = null!;
}
