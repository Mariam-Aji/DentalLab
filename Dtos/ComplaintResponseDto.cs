using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos.Complaints
{
    public class ComplaintResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Text { get; set; } = null!;
        public ComplaintDestination Destination { get; set; }
        public string TargetName { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
    }
}