using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum ConnectionRequestStatus { Pending, Accepted, Rejected }

public class ConnectionRequest
{
    [Key]
    public int Id { get; set; }
    public int FromDentistId { get; set; }
    public int ToLabId { get; set; }
    public ConnectionRequestStatus Status { get; set; } = ConnectionRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User FromDentist { get; set; } = null!;
    public Lab ToLab { get; set; } = null!;
}