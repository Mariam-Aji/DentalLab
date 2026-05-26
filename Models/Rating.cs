using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public class Rating
{
    [Key]
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    public int ReviewerId { get; set; }
    public User? Reviewer { get; set; }
    public int Overall { get; set; }
    public int TimeCommitment { get; set; }
    public int Quality { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}