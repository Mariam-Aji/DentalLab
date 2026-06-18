namespace DentalLab.Api.Dtos;

public class OrderStatusCountDto
{
    /// <summary>اسم الحالة بالإنجليزي (Pending, Accepted, InDesign ...)</summary>
    public string Status { get; set; } = "";

    /// <summary>عدد الطلبات في هذه الحالة</summary>
    public int Count { get; set; }
}
