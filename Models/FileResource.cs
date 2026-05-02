using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum FileType { PhotoBefore, PhotoAfter, XRay, DigitalScan, Other }

public class FileResource
{
    [Key]
    public int Id { get; set; }
    public string Path { get; set; } = null!;
    public FileType Type { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int? BlogPostId { get; set; }
    public int? CaseOrderId { get; set; }
    public int? PatientId { get; set; }

    // Navigation
    public BlogPost? BlogPost { get; set; }
    public CaseOrder? CaseOrder { get; set; }
    public Patient? Patient { get; set; }
}