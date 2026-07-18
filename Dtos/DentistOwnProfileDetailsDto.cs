namespace DentalLab.Api.Dtos
{
    public class DentistOwnProfileDetailsDto
    {
        public int DentistId { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? CityPlace { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
