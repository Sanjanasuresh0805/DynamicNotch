namespace DynamicNotch.Models
{
    public class CalendarDay
    {
        public int Day { get; set; }
        public string DayOfWeekShort { get; set; } = "";
        public string DayOfWeekLetter { get; set; } = "";   // NEW: single letter "M", "T", etc.
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsCurrentMonth { get; set; }
    }
}