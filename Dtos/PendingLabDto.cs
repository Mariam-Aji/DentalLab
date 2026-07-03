using System;
using System.Collections.Generic;

namespace DentalLab.Api.Dtos
{
    public class PendingLabDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NamePlace { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public string? VerificationDocumentPath { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? LabId { get; set; }
        public int YearsOfExperience { get; set; }
        public List<string> Specialties { get; set; } = new();
    }
}