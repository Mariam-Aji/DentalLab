namespace DentalLab.Api.Settings;

public class OtpSettings
{
    public int Length { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 10;
}
