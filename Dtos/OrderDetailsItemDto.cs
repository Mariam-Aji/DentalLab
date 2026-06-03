namespace DentalLab.Api.Dtos
{
    public class OrderDetailsItemDto
    {
        public int ItemId { get; set; }
        public string CompensationType { get; set; } = "";
        public List<int> ToothNumbers { get; set; } = new();
    }
}
