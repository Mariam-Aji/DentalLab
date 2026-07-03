using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models
{
    public class OrderInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderInvoiceId { get; set; }
        public OrderInvoice? OrderInvoice { get; set; }

        [Required]
        public string CompensationType { get; set; } = string.Empty;

        public string ToothNumbers { get; set; } = string.Empty; 
        public decimal UnitPrice { get; set; }
        public int TeethCount { get; set; }
        public decimal LineTotal { get; set; }
    }
}