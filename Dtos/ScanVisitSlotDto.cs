public class ScanVisitSlotDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public string TimeFormatted =>
        DateTime.Today.Add(AppointmentTime).ToString("hh:mm tt");
    //
}