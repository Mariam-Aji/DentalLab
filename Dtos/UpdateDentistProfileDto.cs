namespace DentalLab.Api.Dtos
{
    public class UpdateDentistProfileDto
    {
        public string? Phone { get; set; }
        public string? CityPlace { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}
