// Models/OrderInvoice.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models
{
    public class OrderInvoice
    {
        [Key]
        public int Id { get; set; }

        public int? CaseOrderId { get; set; }
        public CaseOrder? CaseOrder { get; set; }

        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderInvoiceItem> InvoiceItems { get; set; } = new();
    }
}