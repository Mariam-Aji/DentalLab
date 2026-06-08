namespace DentalLab.Api.Dtos;

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? NamePlace { get; set; }
    public string? AddressPlace { get; set; }
    public string? CityPlace { get; set; }
    public string? CountryPlace { get; set; }
}