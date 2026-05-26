using System.ComponentModel.DataAnnotations;
using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos;

public class LabRegistrationDto : IValidatableObject
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

  
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, 60)]
    public int? YearsOfExperience { get; set; }

    public List<string>? Specialties { get; set; }

    public List<string>? Materials { get; set; }

    public AvailabilityStatus? Availability { get; set; }

    public bool HasScanVisitService { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required.", new[] { nameof(Password) });
        }

        if (Specialties != null && Specialties.Count > 0)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Specialties.Count; i++)
            {
                var item = Specialties[i];
                if (string.IsNullOrWhiteSpace(item))
                {
                    yield return new ValidationResult("Specialties cannot contain empty values.", new[] { nameof(Specialties) });
                    break;
                }
                if (item.Length > 100)
                {
                    yield return new ValidationResult("Specialty length must be 100 characters or less.", new[] { nameof(Specialties) });
                    break;
                }
                if (!set.Add(item.Trim()))
                {
                    yield return new ValidationResult("Specialties cannot contain duplicates.", new[] { nameof(Specialties) });
                    break;
                }
            }
        }

        if (Materials != null && Materials.Count > 0)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Materials.Count; i++)
            {
                var item = Materials[i];
                if (string.IsNullOrWhiteSpace(item))
                {
                    yield return new ValidationResult("Materials cannot contain empty values.", new[] { nameof(Materials) });
                    break;
                }
                if (item.Length > 100)
                {
                    yield return new ValidationResult("Material length must be 100 characters or less.", new[] { nameof(Materials) });
                    break;
                }
                if (!set.Add(item.Trim()))
                {
                    yield return new ValidationResult("Materials cannot contain duplicates.", new[] { nameof(Materials) });
                    break;
                }
            }
        }
    }
}
