using DentalLab.Api.Models;
using System.Text.Json.Serialization;

namespace DentalLab.Api.Dtos
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImpressionType? ImpressionType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompensationType? CompensationType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? ToothNumbers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? TotalEstimatedPrice { get; set; }
    }
}