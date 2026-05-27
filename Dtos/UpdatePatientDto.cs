using DentalLab.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos
{
    public class UpdatePatientDto
    {
        [Required(ErrorMessage = "اسم المريض الكامل مطلوب")]
        public string FullName { get; set; } = null!;

        public int? Age { get; set; }

        public string? ClinicalNotes { get; set; }

        public List<string> ProcessedTeeth { get; set; } = new();

        public List<IFormFile>? NewPhotos { get; set; }

        public FileType NewPhotosType { get; set; } = FileType.Other;
    }
}