namespace DentalLab.Api.Dtos;

public class CompensationDemandChartDto
{
    public string CompensationType { get; set; } = null!; 
    public int RequestCount { get; set; }              
    public int TotalTeethCount { get; set; }              
}