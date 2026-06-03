using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum BlogPostType { CommunityDiscussionLab, CommunityDiscussionDoctor }
public enum BlogPostStatus { Pending, Approved, Rejected }
public class BlogPost
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Content { get; set; } = null!;
    public BlogPostType Type { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public bool IsSensitiveRedacted { get; set; } = true;
    public List<FileResource> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BlogPostStatus Status { get; set; } = BlogPostStatus.Pending;
}