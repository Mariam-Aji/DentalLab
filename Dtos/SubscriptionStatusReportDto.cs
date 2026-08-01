namespace DentalLab.Api.Dtos
{
    public class SubscriptionStatusReportDto
    {
        public List<ExpiringLabDto> ExpiringSoonLabs { get; set; } = new();
        public LabsStatusDistributionDto StatusDistribution { get; set; } = new();
        public RetentionRateReportDto RetentionStats { get; set; } = new();
    }
}
