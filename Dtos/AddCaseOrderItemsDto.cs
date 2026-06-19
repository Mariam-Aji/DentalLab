namespace DentalLab.Api.Dtos;

public class AddCaseOrderItemsDto
{
    // مصفوفة لأنواع التعويضات المرسلة
    public List<int> CompensationTypes { get; set; } = new();

    // مصفوفة لنصوص الأسنان المقابلة لها (كل نص يحتوي على الأسنان مفصولة بفاصلة مثل "14,15")
    public List<string> ToothNumbersGrouped { get; set; } = new();
}