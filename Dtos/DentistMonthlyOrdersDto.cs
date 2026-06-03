namespace DentalLab.Api.Dtos;

public class DentistMonthlyOrdersDto
{
    public int DentistId { get; set; }
    public string DentistName { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalOrders { get; set; }
}