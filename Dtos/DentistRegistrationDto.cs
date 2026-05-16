using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class DentistRegistrationDto : IValidatableObject
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = null!;

    [Required]
    [Phone]
    [RegularExpression(@"^\+?[0-9\s\-]{7,30}$", ErrorMessage = "Phone must contain only digits, spaces, + or - and be 7-30 characters long.")]
    [MaxLength(30)]
    public string Phone { get; set; } = null!;

    [MaxLength(200)]
    public string? NamePlace { get; set; }

    [MaxLength(300)]
    public string? AddressPlace { get; set; }

    [MaxLength(120)]
    public string? CityPlace { get; set; }

    [MaxLength(120)]
    public string? CountryPlace { get; set; }

    [Required]
    public IFormFile VerificationDocument { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email is required.", new[] { nameof(Email) });
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required.", new[] { nameof(Password) });
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            yield return new ValidationResult("Phone is required.", new[] { nameof(Phone) });
        }

        if (VerificationDocument == null || VerificationDocument.Length == 0)
        {
            yield return new ValidationResult("Verification document is required.", new[] { nameof(VerificationDocument) });
        }
    }
}
