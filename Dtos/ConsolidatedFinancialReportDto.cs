namespace DentalLab.Api.Dtos.Reports
{
    public class ConsolidatedFinancialReportDto
    {
        public decimal PaidAdsTotalRevenue { get; set; }
        public int PaidAdsCount { get; set; }

        public decimal ActiveSubscriptionsTotalRevenue { get; set; }
        public int ActiveSubscriptionsCount { get; set; }

        public decimal TotalOverallRevenue => PaidAdsTotalRevenue + ActiveSubscriptionsTotalRevenue;
    }
}