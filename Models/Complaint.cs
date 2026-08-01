using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalLab.Api.Models
{
    public enum ComplaintDestination
    {
        Admin,
        Lab
    }

    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public ComplaintDestination Destination { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Text { get; set; } = null!;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        // معرف المخبر المستهدف (اختياري - يُملأ فقط إذا كانت الوجهة Lab)
        public int? TargetLabId { get; set; }

        [ForeignKey("TargetLabId")]
        public Lab? TargetLab { get; set; }

        public int? AdminId { get; set; } = 1;

        [ForeignKey("AdminId")]
        public User? Admin { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Reply { get; set; }

        public DateTime? RepliedAtUtc { get; set; }
    }
}