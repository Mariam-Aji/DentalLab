namespace DentalLab.Api.Dtos;

public class ConnectedDoctorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}
