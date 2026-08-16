namespace DentalLab.Api.Dtos
{
    public class PatientFileResponseDto
    {
        public int FileId { get; set; }
        public string Path { get; set; } = null!;
        public string FileType { get; set; } = null!;
    }
}
