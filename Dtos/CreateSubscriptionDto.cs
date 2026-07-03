using System;

namespace DentalLab.Api.Dtos
{
    public class CreateSubscriptionDto
    {
        public decimal Amount { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
    }
}