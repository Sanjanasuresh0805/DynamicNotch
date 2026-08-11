namespace DynamicNotch.Models;

public class AppSettings
{
    public bool IsFirstRun { get; set; } = true;
    public bool RunAtStartup { get; set; } = false;
    public double IslandWidth { get; set; } = 860;
    public double IslandHeight { get; set; } = 160;
    public double CollapsedWidth { get; set; } = 220;
    public double CollapsedHeight { get; set; } = 38;
    public double Opacity { get; set; } = 1.0;
    public bool ShowCalendar { get; set; } = true;
    public bool ShowMedia { get; set; } = true;
    public bool ShowMirror { get; set; } = true;
}