using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos;

public class LoginResponseDto
{
    public int UserId { get; set; }
    public int? LabId { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; }
    public string AccessMode { get; set; } = "ReadOnly";
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    public string? Message { get; set; }
}
