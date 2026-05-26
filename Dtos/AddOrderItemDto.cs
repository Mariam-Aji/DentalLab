using DentalLab.Api.Models;

public class AddOrderItemDto
{
    public CompensationType CompensationType { get; set; }
    public List<int> ToothNumbers { get; set; } = new();
}