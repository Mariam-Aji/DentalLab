namespace DentalLab.Api.Dtos
{
    public class RetentionRateReportDto
    {
        public int TotalSubscribedLabs { get; set; }      // المخابر التي اشتركت على الأقل مرة واحدة
        public int RenewedLabsCount { get; set; }          // المخابر التي جددت مرتين أو أكثر
        public double RetentionRatePercentage { get; set; } // نسبة الاحتفاظ
    }
}
