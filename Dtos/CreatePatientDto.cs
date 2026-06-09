using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Dtos
{
    public class CreatePatientDto
    {
        public int PatientId { get; set; }
        public int CaseOrderId { get; set; }
        public string FullName { get; set; } = null!;

        public int? Age { get; set; }

        public string? ClinicalNotes { get; set; }

        public List<string> ProcessedTeeth { get; set; } = new();
        //
    }
}