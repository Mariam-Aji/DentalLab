using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalLab.Api.Models;

public enum EmailOtpPurpose
{
    EmailVerification,
    PasswordReset
}

public class EmailOtp
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = null!;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    [Required]
    public EmailOtpPurpose Purpose { get; set; } = EmailOtpPurpose.EmailVerification;

    public DateTime? UsedAtUtc { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
