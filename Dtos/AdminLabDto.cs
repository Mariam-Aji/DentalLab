namespace DentalLab.Api.DTOs
{
    public class AdminLabDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int YearsOfExperience { get; set; }
        public List<string> Specialties { get; set; } = new();
        public List<string> Materials { get; set; } = new();
        public string Availability { get; set; } = string.Empty; // لعرض حالة التوفر كنص
        public bool HasScanVisitService { get; set; }
        public double AverageRating { get; set; }
        public DateTime? SubscriptionStartUtc { get; set; }
        public DateTime? SubscriptionEndUtc { get; set; }

        // معلومات صاحب المخبر (User Owner)
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string LabNamePlace { get; set; } = string.Empty;
        public string AddressPlace { get; set; } = string.Empty;
        public string CityPlace { get; set; } = string.Empty;
        public string CountryPlace { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}