using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class RefreshTokenRequestDto
{
    [Required]
    [MaxLength(200)]
    public string RefreshToken { get; set; } = null!;
}
