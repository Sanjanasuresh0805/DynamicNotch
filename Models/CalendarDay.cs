namespace DynamicNotch.Models;

public class CalendarDay
{
    public int Day { get; set; }
    public string DayOfWeekShort { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsCurrentMonth { get; set; } = true;
}