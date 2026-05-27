using DentalLab.Api.Models;

public class CaseOrderItemDto
{
    public CompensationType CompensationType { get; set; }
    public List<int> ToothNumbers { get; set; } = new();
    //
}