namespace DentalLab.Api.Dtos;

public class LabRatingChartDto
{
    public int LabId { get; set; }
    public string LabName { get; set; } = null!;
    public double AverageOverallRating { get; set; }       // متوسط التقييم الكلي
    public double AverageQualityRating { get; set; }       // متوسط تقييم الجودة
    public double AverageTimeCommitmentRating { get; set; } // متوسط تقييم الالتزام بالوقت
    public int TotalReviews { get; set; }                  // عدد المراجعات الكلي
}