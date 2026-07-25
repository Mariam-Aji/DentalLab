namespace DentalLab.Api.Dtos;

public class MonthlyFinancialGrowthDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = null!;
    public decimal AdsRevenue { get; set; }
    public decimal OrdersRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}