using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum SubscriptionPaymentMethod { ShamCash, Manual }

public class LabSubscriptionPayment
{
    [Key]
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;
    public decimal Amount { get; set; }
    public SubscriptionPaymentMethod Method { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public string? Reference { get; set; }
}
