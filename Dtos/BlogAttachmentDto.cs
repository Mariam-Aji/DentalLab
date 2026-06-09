using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos
{
    public class BlogAttachmentDto
    {
        public int Id { get; set; }
        public string Path { get; set; } = null!;
        public string Type { get; set; } = null!; 
        public DateTime UploadedAt { get; set; }
        public int? BlogPostId { get; set; }
    //
    }
}
