using System.IO;
using System.Windows.Media.Imaging;
using DynamicNotch.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace DynamicNotch.Services;

public class MediaSessionService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action<MediaState>? MediaStateChanged;
    private MediaState _lastState = new();

    public async Task InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            await AttachCurrentSession();
        }
        catch { }
    }

    private async void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args)
    {
        await AttachCurrentSession();
    }

    private async Task AttachCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelineChanged;
        }

        _currentSession = _manager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelineChanged;
            await UpdateMediaState();
        }
        else
        {
            _lastState = new MediaState();
            MediaStateChanged?.Invoke(_lastState);
        }
    }

    private async void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args) => await UpdateMediaState();

    private async void OnPlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args) => await UpdateMediaState();

    private async void OnTimelineChanged(
        GlobalSystemMediaTransportControlsSession sender,
        TimelinePropertiesChangedEventArgs args) => await UpdateMediaState();

    private async Task UpdateMediaState()
    {
        if (_currentSession == null) return;

        try
        {
            var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();
            var playbackInfo = _currentSession.GetPlaybackInfo();
            var timeline = _currentSession.GetTimelineProperties();

            var state = new MediaState
            {
                Title = mediaProperties?.Title ?? string.Empty,
                Artist = mediaProperties?.Artist ?? string.Empty,
                AlbumTitle = mediaProperties?.AlbumTitle ?? string.Empty,
                IsPlaying = playbackInfo?.PlaybackStatus ==
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                SourceApp = _currentSession.SourceAppUserModelId ?? string.Empty,
                Position = timeline?.Position ?? TimeSpan.Zero,
                Duration = timeline?.EndTime ?? TimeSpan.Zero
            };

            if (mediaProperties?.Thumbnail != null)
            {
                try
                {
                    var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                    state.Thumbnail = ConvertToBitmapImage(stream);
                }
                catch { }
            }

            _lastState = state;
            MediaStateChanged?.Invoke(state);
        }
        catch { }
    }

    private BitmapImage? ConvertToBitmapImage(IRandomAccessStreamWithContentType stream)
    {
        try
        {
            using var netStream = stream.AsStreamForRead();
            var memoryStream = new MemoryStream();
            netStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = memoryStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 120;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public async Task PlayPause()
    {
        if (_currentSession == null) return;
        try
        {
            var info = _currentSession.GetPlaybackInfo();
            if (info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                await _currentSession.TryPauseAsync();
            else
                await _currentSession.TryPlayAsync();
        }
        catch { }
    }

    public async Task Next()
    {
        if (_currentSession == null) return;
        try { await _currentSession.TrySkipNextAsync(); }
        catch { }
    }

    public async Task Previous()
    {
        if (_currentSession == null) return;
        try { await _currentSession.TrySkipPreviousAsync(); }
        catch { }
    }

    public MediaState GetCurrentState() => _lastState;

    public void Dispose()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelineChanged;
        }
        if (_manager != null)
        {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }
    }
}