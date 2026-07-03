namespace DentalLab.Api.Dtos
{
    public class RenewSubscriptionDto
    {
        public decimal Amount { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
    }
}
