namespace DentalLab.Api.Dtos
{
    public class LabsStatusDistributionDto
    {
        public int ActiveLabsCount { get; set; }
        public int SuspendedLabsCount { get; set; }
        public int TotalLabsCount { get; set; }
        public double ActivePercentage => TotalLabsCount > 0 ? Math.Round((double)ActiveLabsCount / TotalLabsCount * 100, 2) : 0;
        public double SuspendedPercentage => TotalLabsCount > 0 ? Math.Round((double)SuspendedLabsCount / TotalLabsCount * 100, 2) : 0;
    }
}
