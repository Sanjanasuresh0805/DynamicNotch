using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DynamicNotch.Models;
using DynamicNotch.Services;

namespace DynamicNotch.ViewModels
{
    public class IslandViewModel : INotifyPropertyChanged
    {
        // ── Services ──────────────────────────────────────────────
        private readonly MediaSessionService _mediaService;
        private readonly WeatherService _weatherService;

        // ── Timers ────────────────────────────────────────────────
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _calendarTimer;

        // ── Backing fields ────────────────────────────────────────
        private string _mediaTitle = "";
        private string _mediaArtist = "";
        private string _mediaAlbum = "";
        private BitmapImage? _mediaThumbnail;
        private bool _isPlaying;
        private bool _hasMedia;
        private string _sourceApp = "";

        private string _currentTime = "";
        private string _currentMonth = "";
        private string _todayDay = "";
        private string _todayDayOfWeek = "";

        private bool _isMirrorActive;
        private bool _isExpanded;

        // Weather backing fields
        private string _weatherTemp = "--";
        private string _weatherCondition = "Loading...";
        private string _weatherIcon = "🌤";
        private string _weatherCity = "";
        private string _weatherFeelsLike = "--";
        private string _weatherHumidity = "--";

        // ── Constructor ───────────────────────────────────────────
        public IslandViewModel()
        {
            _mediaService = new MediaSessionService();
            _mediaService.MediaStateChanged += OnMediaStateChanged;
            _ = _mediaService.InitializeAsync();
            // Weather service
            _weatherService = new WeatherService();
            _weatherService.WeatherUpdated += OnWeatherUpdated;
            _ = _weatherService.StartAsync();

            // Clock timer - every second
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            // Calendar timer - every minute
            _calendarTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _calendarTimer.Tick += (s, e) => BuildCalendarDays();
            _calendarTimer.Start();

            UpdateClock();
            BuildCalendarDays();
        }

        // ── Media Properties ──────────────────────────────────────
        public string MediaTitle
        {
            get => _mediaTitle;
            set => SetProperty(ref _mediaTitle, value);
        }

        public string MediaArtist
        {
            get => _mediaArtist;
            set => SetProperty(ref _mediaArtist, value);
        }

        public string MediaAlbum
        {
            get => _mediaAlbum;
            set => SetProperty(ref _mediaAlbum, value);
        }

        public BitmapImage? MediaThumbnail
        {
            get => _mediaThumbnail;
            set => SetProperty(ref _mediaThumbnail, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool HasMedia
        {
            get => _hasMedia;
            set => SetProperty(ref _hasMedia, value);
        }

        public string SourceApp
        {
            get => _sourceApp;
            set => SetProperty(ref _sourceApp, value);
        }

        // ── Clock Properties ──────────────────────────────────────
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string CurrentMonth
        {
            get => _currentMonth;
            set => SetProperty(ref _currentMonth, value);
        }

        public string TodayDay
        {
            get => _todayDay;
            set => SetProperty(ref _todayDay, value);
        }

        public string TodayDayOfWeek
        {
            get => _todayDayOfWeek;
            set => SetProperty(ref _todayDayOfWeek, value);
        }

        // ── Calendar ──────────────────────────────────────────────
        public ObservableCollection<CalendarDay> CalendarDays { get; }
            = new ObservableCollection<CalendarDay>();

        // ── Mirror ────────────────────────────────────────────────
        public bool IsMirrorActive
        {
            get => _isMirrorActive;
            set => SetProperty(ref _isMirrorActive, value);
        }

        // ── Expanded state ────────────────────────────────────────
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        // ── Weather Properties ────────────────────────────────────
        public string WeatherTemp
        {
            get => _weatherTemp;
            set => SetProperty(ref _weatherTemp, value);
        }

        public string WeatherCondition
        {
            get => _weatherCondition;
            set => SetProperty(ref _weatherCondition, value);
        }

        public string WeatherIcon
        {
            get => _weatherIcon;
            set => SetProperty(ref _weatherIcon, value);
        }

        public string WeatherCity
        {
            get => _weatherCity;
            set => SetProperty(ref _weatherCity, value);
        }

        public string WeatherFeelsLike
        {
            get => _weatherFeelsLike;
            set => SetProperty(ref _weatherFeelsLike, value);
        }

        public string WeatherHumidity
        {
            get => _weatherHumidity;
            set => SetProperty(ref _weatherHumidity, value);
        }

        // ── Media Commands ────────────────────────────────────────
       public void PlayPauseCommand()
{
    try
    {
        var method = _mediaService.GetType().GetMethod("PlayPauseAsync")
                  ?? _mediaService.GetType().GetMethod("TogglePlayPauseAsync")
                  ?? _mediaService.GetType().GetMethod("PlayPause");
        method?.Invoke(_mediaService, null);
    }
    catch { }
}

public void NextCommand()
{
    try
    {
        var method = _mediaService.GetType().GetMethod("NextAsync")
                  ?? _mediaService.GetType().GetMethod("SkipNextAsync")
                  ?? _mediaService.GetType().GetMethod("Next");
        method?.Invoke(_mediaService, null);
    }
    catch { }
}

public void PreviousCommand()
{
    try
    {
        var method = _mediaService.GetType().GetMethod("PreviousAsync")
                  ?? _mediaService.GetType().GetMethod("SkipPreviousAsync")
                  ?? _mediaService.GetType().GetMethod("Previous");
        method?.Invoke(_mediaService, null);
    }
    catch { }
}

        public void ToggleMirror()
        {
            IsMirrorActive = !IsMirrorActive;
        }

        // ── Private Helpers ───────────────────────────────────────
        private void OnMediaStateChanged(MediaState state)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MediaTitle = state.Title;
                MediaArtist = state.Artist;
                MediaAlbum = state.AlbumTitle;
                MediaThumbnail = state.Thumbnail;
                IsPlaying = state.IsPlaying;
                HasMedia = !string.IsNullOrEmpty(state.Title);
                SourceApp = GetFriendlyAppName(state.SourceApp);
            });
        }

        private void OnWeatherUpdated(WeatherData data)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                WeatherTemp = data.Temperature;
                WeatherCondition = data.Condition;
                WeatherIcon = data.Icon;
                WeatherCity = data.City;
                WeatherFeelsLike = data.FeelsLike;
                WeatherHumidity = data.Humidity;
            });
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("hh:mm tt");
            TodayDay = now.Day.ToString();
            TodayDayOfWeek = now.DayOfWeek.ToString()[..3].ToUpper();
            CurrentMonth = now.ToString("MMM yyyy");
        }

        public void BuildCalendarDays()
        {
            CalendarDays.Clear();
            var today = DateTime.Today;

            for (int offset = -3; offset <= 3; offset++)
            {
                var day = today.AddDays(offset);
                CalendarDays.Add(new CalendarDay
                {
                    Day = day.Day,
                    DayOfWeekShort = day.DayOfWeek.ToString()[..3].ToUpper(),
                    IsToday = offset == 0,
                    IsWeekend = day.DayOfWeek == DayOfWeek.Saturday
                              || day.DayOfWeek == DayOfWeek.Sunday,
                    IsCurrentMonth = day.Month == today.Month
                });
            }
        }

        private static string GetFriendlyAppName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.ToLower() switch
            {
                var s when s.Contains("spotify") => "Spotify",
                var s when s.Contains("chrome") => "YouTube",
                var s when s.Contains("firefox") => "Firefox",
                var s when s.Contains("msedge") => "Edge",
                var s when s.Contains("apple") => "Apple Music",
                var s when s.Contains("vlc") => "VLC",
                var s when s.Contains("netflix") => "Netflix",
                var s when s.Contains("groove") => "Groove",
                var s when s.Contains("wmplayer") => "WMP",
                _ => raw
            };
        }

        public void Cleanup()
        {
            _clockTimer.Stop();
            _calendarTimer.Stop();
            _weatherService.Stop();
            _mediaService.Dispose();
        }

        // ── INotifyPropertyChanged ────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value,
            [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}