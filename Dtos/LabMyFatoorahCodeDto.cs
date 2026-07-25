namespace DentalLab.Api.Dtos;

/// <summary>
/// DTO لعرض كود حساب MyFatoorah الخاص بالمخبر
/// </summary>
public class LabMyFatoorahCodeResponseDto
{
    public long? MyFatoorahSupplierCode { get; set; }
}

/// <summary>
/// DTO لتعديل كود حساب MyFatoorah الخاص بالمخبر
/// </summary>
public class UpdateLabMyFatoorahCodeDto
{
    /// <summary>
    /// كود الحساب على منصة MyFatoorah — أرسل null لحذفه
    /// </summary>
    public long? MyFatoorahSupplierCode { get; set; }
}
