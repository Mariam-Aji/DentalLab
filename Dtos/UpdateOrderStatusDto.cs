using DentalLab.Api.Models;
using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Dtos;

public class UpdateOrderStatusDto
{
    /// <summary>الحالة الجديدة للطلب (Pending, Accepted, InDesign, InProduction, Ready, Delivered ...)</summary>
    public CaseStatus Status { get; set; }

    /// <summary>ملاحظات اختيارية تُضاف مع التحديث</summary>
    public string? Notes { get; set; }

    /// <summary>صورة نتيجة العمل (اختيارية) - jpg / jpeg / png / webp</summary>
    public IFormFile? ResultImage { get; set; }
}
