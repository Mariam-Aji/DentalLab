using System;
using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos.Complaints
{
    public class ComplaintDetailsDto
    {
        public int Id { get; set; }
        public ComplaintDestination Destination { get; set; }
        public string Title { get; set; } = null!;
        public string Text { get; set; } = null!;
        public int UserId { get; set; }
        public int? TargetLabId { get; set; }
        //public int? AdminId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Reply { get; set; }
        public DateTime? RepliedAtUtc { get; set; }
        public string? RepliedBy { get; set; }
    }
}