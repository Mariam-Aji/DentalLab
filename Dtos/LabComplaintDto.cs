namespace DentalLab.Api.Dtos;

/// <summary>
/// شكوى واردة للمخبر مع الرد إن وُجد
/// </summary>
public class LabComplaintDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public string DentistName { get; set; } = string.Empty;
    public int DentistId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>رد المخبر — null إذا لم يرد بعد</summary>
    public string? Reply { get; set; }
    public DateTime? RepliedAtUtc { get; set; }
}

/// <summary>
/// طلب رد المخبر على الشكوى
/// </summary>
public class LabComplaintReplyDto
{
    public string Reply { get; set; } = string.Empty;
}
