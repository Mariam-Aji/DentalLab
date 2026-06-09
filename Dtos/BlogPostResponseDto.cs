using System;
using System.Collections.Generic;

namespace DentalLab.Api.Dtos
{
    public class BlogPostResponseDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int AuthorId { get; set; }

        public string AuthorName { get; set; } = null!;

        public bool IsSensitiveRedacted { get; set; }
        public string Status { get; set; } = null!;
        public string? ReviewMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BlogAttachmentDto> Attachments { get; set; } = new();
    }
}