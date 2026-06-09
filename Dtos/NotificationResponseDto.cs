using System;

namespace DentalLab.Api.Dtos;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public string Message { get; set; } = null!;
    //public string Type { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int? BlogPostId { get; set; }
    public string? BlogPostType { get; set; } 
}