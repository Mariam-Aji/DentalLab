namespace DentalLab.Api.DTOs
{
    public class PatientCaseOrderDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ImpressionStage { get; set; } = string.Empty;
        public string ImpressionType { get; set; } = string.Empty;
        public string? Shade { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsTemporary { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public decimal? FinalPrice { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // معلومات الطبيب والمخبر والمريض المرتبط
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int CreatedById { get; set; }
        public string DentistName { get; set; } = string.Empty;
        public int? AssignedLabId { get; set; }
        public string? LabName { get; set; }
    }
}