using System.Text.Json.Serialization;

namespace DentalLab.Api.Dtos;

public class LabDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ProfilePictureUrl { get; set; } 
    public bool? IsConnected { get; set; }
    public double? AverageRating { get; set; }
    [JsonPropertyName("ratingsCount")]
    public int? RatingsCount { get; set; }
    public string? Phone { get; set; }
    public string? AddressPlace { get; set; }
    public string? CityPlace { get; set; }
    public string? CountryPlace { get; set; }
}
