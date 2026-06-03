namespace DentalLab.Api.Dtos;

public class LabMonthlyOrdersDto
{
    public int LabId { get; set; }
    public string LabName { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalOrders { get; set; }
}