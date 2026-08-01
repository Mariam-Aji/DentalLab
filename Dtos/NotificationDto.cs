namespace DentalLab.Api.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? OrderId { get; set; }
    public int? LabId { get; set; }
    public int? BlogPostId { get; set; }
}