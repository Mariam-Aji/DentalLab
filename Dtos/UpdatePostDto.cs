using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DentalLab.Api.Dtos
{
    public class UpdatePostDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool? IsSensitiveRedacted { get; set; }

        public List<IFormFile>? NewDocumentFiles { get; set; }
    }
}