namespace DentalLab.Api.Dtos
{
    public class ExpiringLabDto
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public DateTime SubscriptionEndUtc { get; set; }
        public int DaysRemaining { get; set; }
    }
}
