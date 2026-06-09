using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos;

public class CreateADSClientDto
{
    public string Name { get; set; } = null!;

    public string? Phone { get; set; }
    public string? NamePlace { get; set; }
    public string? AddressPlace { get; set; }
    public string? CityPlace { get; set; }
    public string? CountryPlace { get; set; }
}