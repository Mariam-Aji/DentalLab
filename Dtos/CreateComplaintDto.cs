using System.ComponentModel.DataAnnotations;
using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos.Complaints
{
    public class CreateComplaintDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Text { get; set; } = null!;
    }
}