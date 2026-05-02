using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum UserRole { Dentist, Lab, Admin }
public enum AccountStatus { PendingVerification, PendingAdminApproval, Active, ReadOnly, Suspended }

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public string Email { get; set; } = null!;
    [Required]
    public string PasswordHash { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NamePlace { get; set; }
    public string? AddressPlace { get; set; }
    public string? CityPlace { get; set; }
    public string? CountryPlace { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.PendingVerification;
    public string? VerificationDocumentPath { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<CaseOrder> CreatedCases { get; set; } = new();
    public Lab? LabProfile { get; set; }
    public List<ConnectionRequest> SentConnectionRequests { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}