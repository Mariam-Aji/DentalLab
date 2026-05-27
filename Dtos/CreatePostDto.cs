using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos
{
    public class CreatePostDto
    {
        [Required(ErrorMessage = "عنوان المقال مطلوب")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "محتوى المقال مطلوب")]
        public string Content { get; set; } = null!;

        public bool IsSensitiveRedacted { get; set; }

        public List<IFormFile>? DocumentFiles { get; set; }
    }
}