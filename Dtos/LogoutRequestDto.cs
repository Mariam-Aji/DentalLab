using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class LogoutRequestDto
{
    [Required]
    [MaxLength(200)]
    public string RefreshToken { get; set; } = null!;
}
