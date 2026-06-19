using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos
{
    public class OrderItemUpdateDto
    {
        public CompensationType CompensationType { get; set; }
        public List<int> ToothNumbers { get; set; } = new();
    }
}
