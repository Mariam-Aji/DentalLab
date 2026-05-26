using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class LabGalleryUploadDto : IValidatableObject
{
    [Required]
    public List<IFormFile> Images { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Images == null || Images.Count == 0)
        {
            yield return new ValidationResult("At least one image is required.", new[] { nameof(Images) });
        }
    }
}
