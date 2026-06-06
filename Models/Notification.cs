using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalLab.Api.Models;

public enum NotificationType { OrderAccepted, OrderRejected, InfoRequested, StatusChanged, PriceSet, DeliveryDue, OrderCompleted, Cancellation, ScanVisitConfirmed }

public class Notification
{
    [Key]
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Recipient { get; set; } = null!;
    public int? BlogPostId { get; set; }

    [ForeignKey(nameof(BlogPostId))]
    public BlogPost? BlogPost { get; set; }
}