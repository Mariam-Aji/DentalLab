namespace DentalLab.Api.Dtos;

public class DentistNotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>معرف الطلبية المرتبطة — null إذا لم يكن الإشعار مرتبطاً بطلبية</summary>
    public int? OrderId { get; set; }

    /// <summary>معرف المخبر المرتبط — null إذا لم يكن الإشعار مرتبطاً بمخبر</summary>
    public int? LabId { get; set; }

    /// <summary>معرف المنشور المرتبط — null إذا لم يكن الإشعار مرتبطاً بمنشور</summary>
    public int? BlogPostId { get; set; }
}
