using System;

namespace DentalLab.Api.Dtos
{
    public class PendingDentistDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NamePlace { get; set; } 
        public string? CityPlace { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public string? VerificationDocumentPath { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}