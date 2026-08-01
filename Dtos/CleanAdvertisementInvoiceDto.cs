namespace DentalLab.Api.DTOs
{
    public class CleanAdvertisementInvoiceDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Target { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } // إضافة علامة الاستفهام هنا
        public decimal? Price { get; set; }
        public bool IsActive { get; set; }
      
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        // معلومات المستخدم (صاحب الإعلان) بشكل نظيف وخالٍ من الحساسات
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public string NamePlace { get; set; }
        public string AddressPlace { get; set; }
        public string CityPlace { get; set; }
        public string CountryPlace { get; set; }
    }
}