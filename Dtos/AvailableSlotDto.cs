public class AvailableSlotDto
{
    public int SlotId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Period { get; set; } = "";
}