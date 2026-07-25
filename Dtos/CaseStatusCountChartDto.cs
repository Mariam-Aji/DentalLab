namespace DentalLab.Api.Dtos;

using DentalLab.Api.Models;

public class CaseStatusCountChartDto
{
    public string StatusName { get; set; } = null!; // اسم الحالة (مثل Pennding, Ready...)
    public CaseStatus Status { get; set; }          // قيمة الـ Enum الفعلية
    public int OrderCount { get; set; }             // عدد الطلبات في هذه الحالة
}