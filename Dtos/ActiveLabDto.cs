using System;

namespace DentalLab.Api.Dtos
{
    public class ActiveLabDto
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? SubscriptionStartUtc { get; set; }
        public DateTime? SubscriptionEndUtc { get; set; }
        public int RemainingDays { get; set; }
    }
}