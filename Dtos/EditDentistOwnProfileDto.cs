namespace DentalLab.Api.Dtos
{
    public class EditDentistOwnProfileDto
    {
        public string? Phone { get; set; }
        public string? CityPlace { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}
