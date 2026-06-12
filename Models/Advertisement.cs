using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalLab.Api.Models;

public enum TargetAudience
{
    Dentists,
    Labs,
    Both
}

public class Advertisement
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    // 🎯 الإضافة هنا: ربط حقل الجمهور المستهدف بالـ Enum مباشرة
    [Required]
    public TargetAudience Target { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int UserId { get; set; }
    public decimal? Price { get; set; }

    // Navigation Property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}