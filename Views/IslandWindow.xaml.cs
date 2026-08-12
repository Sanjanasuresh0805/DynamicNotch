using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DynamicNotch.Services;
using DynamicNotch.ViewModels;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;

namespace DynamicNotch.Views
{
    public partial class IslandWindow : Window
    {
        // ── Layout constants ───────────────────────────────────────
        private const double CollapsedWidth  = 220;
        private const double CollapsedHeight = 40;
        private const double ExpandedWidth   = 680;
        private const double ExpandedHeight  = 140;
        private const int    ExpandDelayMs   = 120;
        private const int    CollapseDelayMs = 800;

        // ── State flags ────────────────────────────────────────────
        private bool _isExpanded          = false;
        private bool _isAnimating         = false;
        private bool _isHiddenByFullscreen = false;
        private bool _isHiddenByHotkey     = false;

        // ── ViewModel ──────────────────────────────────────────────
        private readonly IslandViewModel _vm;

        // ── Timers ─────────────────────────────────────────────────
        private readonly DispatcherTimer _expandTimer;
        private readonly DispatcherTimer _collapseTimer;
        private readonly DispatcherTimer _hoverCheckTimer;
        private readonly DispatcherTimer _topmostGuardTimer;

        // ── Services ───────────────────────────────────────────────
        private readonly FullscreenDetectorService _fullscreenDetector;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly BatteryService _batteryService;
        private readonly EventsService _eventsService;

        // ── Equalizer animation ────────────────────────────────────
        private Storyboard? _equalizerStoryboard;

        // ── Webcam fields ──────────────────────────────────────────
        private MediaCapture?      _mediaCapture;
        private MediaFrameReader?  _frameReader;
        private WriteableBitmap?   _webcamBitmap;
        private bool               _webcamRunning = false;

        // ── Win32 ──────────────────────────────────────────────────
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT pt);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE  = 0x0002;
        private const uint SWP_NOSIZE  = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        // ─────────────────────────────────────────────────────────
        public IslandWindow()
        {
            InitializeComponent();

            _vm = new IslandViewModel();
            DataContext = _vm;

            PositionWindow();

            // Init services
            _fullscreenDetector = new FullscreenDetectorService();
            _fullscreenDetector.FullscreenChanged += OnFullscreenChanged;

            _hotkeyService = new GlobalHotkeyService(hotkeyId: 9001);
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            _batteryService = new BatteryService();
            _batteryService.BatteryUpdated += OnBatteryUpdated;

            _eventsService = new EventsService();
            _eventsService.EventUpdated += OnEventUpdated;

            // Apply Win32 window style + register hotkey once loaded
            Loaded += (s, e) =>
            {
                WindowStyleHelper.MakeIslandWindow(this);

                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    _fullscreenDetector.SetOurWindowHandle(hwnd);
                }

                // Register Ctrl + Shift + N as GLOBAL hotkey (works from ANY app)
                _hotkeyService.Register(
                    GlobalHotkeyService.MOD_CONTROL | GlobalHotkeyService.MOD_SHIFT,
                    (uint)KeyInterop.VirtualKeyFromKey(Key.N));

                //_fullscreenDetector.Start();

                // Start battery + events monitor
                _batteryService.Start();
                _eventsService.Start();

                // Initialize equalizer animation state
                UpdateEqualizerAnimation();
            };

            // ── Timers ──
            _expandTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ExpandDelayMs) };
            _expandTimer.Tick += (s, e) => { _expandTimer.Stop(); ExpandIsland(); };

            _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CollapseDelayMs) };
            _collapseTimer.Tick += (s, e) => { _collapseTimer.Stop(); CollapseIsland(); };

            _hoverCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _hoverCheckTimer.Tick += HoverCheck_Tick;
            _hoverCheckTimer.Start();

            _topmostGuardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _topmostGuardTimer.Tick += TopmostGuard_Tick;
            _topmostGuardTimer.Start();

            // Watch IsPlaying to start/stop equalizer animation
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IslandViewModel.IsPlaying))
                {
                    Dispatcher.Invoke(() => UpdateEqualizerAnimation());
                }
            };
        }

        // ─────────────────────────────────────────────────────────
        // EQUALIZER ANIMATION
        // ─────────────────────────────────────────────────────────
        private void UpdateEqualizerAnimation()
        {
            if (_vm.IsPlaying)
                StartEqualizerAnimation();
            else
                StopEqualizerAnimation();
        }

        private void StartEqualizerAnimation()
        {
            try
            {
                if (_equalizerStoryboard == null)
                {
                    _equalizerStoryboard = (Storyboard)Resources["EqualizerStoryboard"];
                }
                _equalizerStoryboard?.Begin(this, true);
            }
            catch { }
        }

        private void StopEqualizerAnimation()
        {
            try
            {
                _equalizerStoryboard?.Stop(this);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────
        // BATTERY UPDATE
        // ─────────────────────────────────────────────────────────
        private void OnBatteryUpdated(object? sender, BatteryEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _vm.BatteryPercentage = e.Percentage;
                _vm.IsCharging = e.IsCharging;
                _vm.IsBatteryAvailable = e.IsAvailable;
            });
        }

        private void OnEventUpdated(object? sender, EventUpdatedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _vm.TodayEvent = e.EventText;
            });
        }

        // ─────────────────────────────────────────────────────────
        // Window positioning
        // ─────────────────────────────────────────────────────────
        private void PositionWindow()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            Left = (screenW - CollapsedWidth) / 2;
            Top = 6;  // small gap so top curves are visible
        }

        // ─────────────────────────────────────────────────────────
        // HOTKEY TOGGLE  (Ctrl + Shift + N)
        // ─────────────────────────────────────────────────────────
        private void OnHotkeyPressed()
        {
            Dispatcher.Invoke(() =>
            {
                if (_isHiddenByHotkey)
                {
                    _isHiddenByHotkey = false;
                    if (!_isHiddenByFullscreen)
                        ShowNotch();
                }
                else
                {
                    _isHiddenByHotkey = true;
                    HideNotch();
                }
            });
        }

        // ─────────────────────────────────────────────────────────
        // FULLSCREEN AUTO-HIDE
        // ─────────────────────────────────────────────────────────
        private void OnFullscreenChanged(bool isFullscreen)
        {
            Dispatcher.Invoke(() =>
            {
                if (isFullscreen && !_isHiddenByFullscreen)
                {
                    _isHiddenByFullscreen = true;
                    HideNotch();
                }
                else if (!isFullscreen && _isHiddenByFullscreen)
                {
                    _isHiddenByFullscreen = false;
                    if (!_isHiddenByHotkey)
                        ShowNotch();
                }
            });
        }

        // ─────────────────────────────────────────────────────────
        // SHOW / HIDE
        // ─────────────────────────────────────────────────────────
        private void HideNotch()
        {
            _expandTimer.Stop();
            _collapseTimer.Stop();

            var slideUp = new DoubleAnimation
            {
                From = Top,
                To = -(CollapsedHeight + 5),
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            slideUp.Completed += (s, e) =>
            {
                Visibility = Visibility.Hidden;
            };
            BeginAnimation(TopProperty, slideUp);
        }

        private void ShowNotch()
        {
            Visibility = Visibility.Visible;

            if (_isExpanded)
            {
                _isExpanded = false;
                IslandBorder.Width  = CollapsedWidth;
                IslandBorder.Height = CollapsedHeight;
                IslandBorder.CornerRadius = new CornerRadius(18);
                CollapsedContent.Opacity = 1;
                ExpandedContent.Opacity  = 0;

                double sw = SystemParameters.PrimaryScreenWidth;
                Width  = CollapsedWidth;
                Height = CollapsedHeight;
                Left   = (sw - CollapsedWidth) / 2;
            }

            var slideDown = new DoubleAnimation
            {
                From = -(CollapsedHeight + 5),
                To = 6,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 0.3
                }
            };
            BeginAnimation(TopProperty, slideDown);

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        // ─────────────────────────────────────────────────────────
        // EXPAND / COLLAPSE
        // ─────────────────────────────────────────────────────────
        private void ExpandIsland()
        {
            if (_isExpanded || _isAnimating) return;
            if (_isHiddenByFullscreen || _isHiddenByHotkey) return;
            _isAnimating = true;

            CollapsedContent.Opacity = 1;
            ExpandedContent.Opacity = 0;

            double screenW = SystemParameters.PrimaryScreenWidth;
            Width  = ExpandedWidth;
            Height = ExpandedHeight;
            Left   = (screenW - ExpandedWidth) / 2;

            IslandBorder.CornerRadius = new CornerRadius(18);

            var sb = (Storyboard)Resources["ExpandStoryboard"];
            sb.Completed += (s, e) =>
            {
                _isExpanded  = true;
                _isAnimating = false;
                _vm.IsExpanded = true;
            };
            sb.Begin(IslandBorder, HandoffBehavior.SnapshotAndReplace);
        }

        private void CollapseIsland()
        {
            if (!_isExpanded || _isAnimating) return;
            if (_vm.IsMirrorActive) return;
            _isAnimating = true;

            CollapsedContent.Opacity = 1;
            ExpandedContent.Opacity = 0;

            var sb = (Storyboard)Resources["CollapseStoryboard"];
            sb.Completed += (s, e) =>
            {
                _isExpanded  = false;
                _isAnimating = false;
                _vm.IsExpanded = false;

                CollapsedContent.Opacity = 1;
                ExpandedContent.Opacity = 0;
                IslandBorder.CornerRadius = new CornerRadius(18);

                double screenW = SystemParameters.PrimaryScreenWidth;
                Width  = CollapsedWidth;
                Height = CollapsedHeight;
                Left   = (screenW - CollapsedWidth) / 2;
            };
            sb.Begin(IslandBorder, HandoffBehavior.SnapshotAndReplace);
        }

        // ─────────────────────────────────────────────────────────
        // HOVER DETECTION
        // ─────────────────────────────────────────────────────────
        private void HoverCheck_Tick(object? sender, EventArgs e)
        {
            if (_isHiddenByFullscreen || _isHiddenByHotkey) return;

            bool inside = IsMouseInsideNotch();

            if (inside && !_isExpanded && !_isAnimating)
            {
                _collapseTimer.Stop();
                if (!_expandTimer.IsEnabled)
                    _expandTimer.Start();
            }
            else if (!inside && _isExpanded && !_isAnimating)
            {
                _expandTimer.Stop();
                if (!_collapseTimer.IsEnabled)
                    _collapseTimer.Start();
            }
            else if (!inside && !_isExpanded)
            {
                _expandTimer.Stop();
            }
        }

        private bool IsMouseInsideNotch()
        {
            if (!GetCursorPos(out POINT pt)) return false;

            var source = PresentationSource.FromVisual(this);
            if (source == null) return false;

            double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            double scaleX = dpiX / 96.0;
            double scaleY = dpiY / 96.0;

            double winLeft   = Left   * scaleX;
            double winTop    = Top    * scaleY;
            double winRight  = winLeft + Width  * scaleX;
            double winBottom = winTop  + Height * scaleY;

            return pt.X >= winLeft && pt.X <= winRight
                && pt.Y >= winTop  && pt.Y <= winBottom;
        }

        // ─────────────────────────────────────────────────────────
        // TOPMOST GUARD
        // ─────────────────────────────────────────────────────────
        private void TopmostGuard_Tick(object? sender, EventArgs e)
        {
            if (_isHiddenByFullscreen || _isHiddenByHotkey) return;
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────
        // BUTTON HANDLERS
        // ─────────────────────────────────────────────────────────
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
            => _vm.PlayPauseCommand();

        private void NextButton_Click(object sender, RoutedEventArgs e)
            => _vm.NextCommand();

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
            => _vm.PreviousCommand();

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var sw = new SettingsWindow();
            sw.Show();
        }

        private void MirrorButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.ToggleMirror();
            if (_vm.IsMirrorActive)
                _ = StartWebcamAsync();
            else
                StopWebcam();
        }

        // ─────────────────────────────────────────────────────────
        // WEBCAM
        // ─────────────────────────────────────────────────────────
        private async Task StartWebcamAsync()
        {
            try
            {
                var groups = await MediaFrameSourceGroup.FindAllAsync();
                if (groups.Count == 0) return;

                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup            = groups[0],
                    SharingMode            = MediaCaptureSharingMode.SharedReadOnly,
                    StreamingCaptureMode   = StreamingCaptureMode.Video,
                    MemoryPreference       = MediaCaptureMemoryPreference.Cpu
                };

                await _mediaCapture.InitializeAsync(settings);

                MediaFrameSource? colorSource = null;
                foreach (var src in _mediaCapture.FrameSources.Values)
                {
                    if (src.Info.SourceKind == MediaFrameSourceKind.Color)
                    { colorSource = src; break; }
                }
                if (colorSource == null) return;

                _frameReader = await _mediaCapture
                    .CreateFrameReaderAsync(colorSource,
                        Windows.Media.MediaProperties.MediaEncodingSubtypes.Bgra8);
                _frameReader.FrameArrived += FrameReader_FrameArrived;
                await _frameReader.StartAsync();

                _webcamRunning = true;
                WebcamEllipse.Visibility = Visibility.Visible;
                WebcamIcon.Visibility    = Visibility.Collapsed;
            }
            catch
            {
                _vm.IsMirrorActive = false;
            }
        }

        private void StopWebcam()
        {
            _webcamRunning = false;
            WebcamEllipse.Visibility = Visibility.Collapsed;
            WebcamIcon.Visibility    = Visibility.Visible;

            _frameReader?.StopAsync().AsTask().ContinueWith(_ =>
            {
                _frameReader?.Dispose();
                _frameReader = null;
            });
            _mediaCapture?.Dispose();
            _mediaCapture = null;
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender,
            MediaFrameArrivedEventArgs args)
        {
            using var frame = sender.TryAcquireLatestFrame();
            if (frame == null) return;
            var vFrame = frame.VideoMediaFrame;
            if (vFrame == null) return;
            using var bitmap = vFrame.SoftwareBitmap;
            if (bitmap == null) return;

            using var converted = SoftwareBitmap.Convert(bitmap,
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            int w = converted.PixelWidth;
            int h = converted.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            converted.CopyToBuffer(
                System.Runtime.InteropServices.WindowsRuntime
                    .WindowsRuntimeBufferExtensions.AsBuffer(pixels));

            Dispatcher.Invoke(() =>
            {
                if (!_webcamRunning) return;
                if (_webcamBitmap == null || _webcamBitmap.PixelWidth != w
                                          || _webcamBitmap.PixelHeight != h)
                {
                    _webcamBitmap = new WriteableBitmap(w, h, 96, 96,
                        PixelFormats.Bgra32, null);
                    WebcamBrush.ImageSource = _webcamBitmap;
                }
                _webcamBitmap.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);
            });
        }

        // ─────────────────────────────────────────────────────────
        // WINDOW OVERRIDES
        // ─────────────────────────────────────────────────────────
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;
            base.OnStateChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _expandTimer.Stop();
            _collapseTimer.Stop();
            _hoverCheckTimer.Stop();
            _topmostGuardTimer.Stop();
            _fullscreenDetector.Stop();
            _hotkeyService.Dispose();
            _eventsService.Stop();
            _batteryService.Stop();
            StopEqualizerAnimation();
            StopWebcam();
            _vm.Cleanup();
            base.OnClosed(e);
        }
    }
}