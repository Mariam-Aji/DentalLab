namespace DentalLab.Api.DTOs
{
    public class CleanOrderInvoiceDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public decimal FinalPrice { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // معلومات الطبيب كاملة ونظيفة
        public string DentistName { get; set; }
        public string DentistEmail { get; set; }
        public string DentistPhone { get; set; }
        public string ClinicName { get; set; }
        public string AddressPlace { get; set; }
        public string CityPlace { get; set; }
        public string CountryPlace { get; set; }

        public string LabName { get; set; }
        public List<string> Items { get; set; } = new();
    }
}