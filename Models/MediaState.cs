using System.Windows.Media.Imaging;

namespace DynamicNotch.Models;

public class MediaState
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public BitmapImage? Thumbnail { get; set; }
    public bool IsPlaying { get; set; }
    public string SourceApp { get; set; } = string.Empty;
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}