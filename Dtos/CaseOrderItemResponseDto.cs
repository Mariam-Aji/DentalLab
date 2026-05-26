using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos
{
    public class CaseOrderItemResponseDto
    {
        public int CaseOrderId { get; set; }
        public int CaseOrderItemId { get; set; }
        public string Status { get; set; } = string.Empty;

        public CompensationType CompensationType { get; set; }
        public List<int> ToothNumbers { get; set; } = new();
    }
}