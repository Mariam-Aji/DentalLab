namespace DentalLab.Api.Dtos;

/// <summary>
/// تفاصيل طبيب — يُستخدم من واجهة المخبر
/// </summary>
public class DentistProfileDto
{
    public int    Id                { get; set; }
    public string Name              { get; set; } = string.Empty;
    public string Email             { get; set; } = string.Empty;
    public string? Phone            { get; set; }
    public string? ClinicName       { get; set; }
    public string? ClinicAddress    { get; set; }
    public string? City             { get; set; }
    public string? Country          { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
