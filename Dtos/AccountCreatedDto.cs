using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos;

public class AccountCreatedDto
{
    public int UserId { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; }
    public int? LabId { get; set; }
}
