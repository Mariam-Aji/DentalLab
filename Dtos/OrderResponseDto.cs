using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }

        //public string Status { get; set; } = string.Empty;

        
        public ImpressionStage ImpressionStage { get; set; }
    }
}