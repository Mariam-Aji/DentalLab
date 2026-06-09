using DentalLab.Api.Models;

public class CaseOrderInvoiceItemDto
{
    public int CaseOrderItemId { get; set; }
    public int CaseOrderId { get; set; }   
    public CompensationType CompensationType { get; set; }

    public List<int> ToothNumbers { get; set; } = new();

    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    //
}