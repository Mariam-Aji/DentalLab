using DentalLab.Api.Dtos;

public class BlogPostResponseDto
{
    public int PostId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int AuthorId { get; set; }
    public bool IsSensitiveRedacted { get; set; }
    public DateTime CreatedAt { get; set; }
    //
    public List<BlogAttachmentDto> Attachments { get; set; } = new();
}