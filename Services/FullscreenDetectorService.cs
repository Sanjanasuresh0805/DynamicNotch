using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    public class FullscreenDetectorService
    {
        // Win32 imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public event Action<bool>? FullscreenChanged;

        private DispatcherTimer? _timer;
        private bool _wasFullscreen = false;
        private IntPtr _ourWindowHandle = IntPtr.Zero;

        public void SetOurWindowHandle(IntPtr hwnd)
        {
            _ourWindowHandle = hwnd;
        }

        public void Start()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            bool isFullscreen = CheckFullscreen();

            if (isFullscreen != _wasFullscreen)
            {
                _wasFullscreen = isFullscreen;
                FullscreenChanged?.Invoke(isFullscreen);
            }
        }

        private bool CheckFullscreen()
        {
            try
            {
                var fgWindow = GetForegroundWindow();

                // Ignore if no foreground window
                if (fgWindow == IntPtr.Zero) return false;

                // Ignore our own window
                if (fgWindow == _ourWindowHandle) return false;

                // Ignore desktop and shell
                if (fgWindow == GetDesktopWindow()) return false;
                if (fgWindow == GetShellWindow()) return false;

                // Ignore Windows taskbar / shell UI elements
                var className = new System.Text.StringBuilder(256);
                GetClassName(fgWindow, className, 256);
                var cls = className.ToString();

                if (cls == "Shell_TrayWnd" ||
                    cls == "WorkerW" ||
                    cls == "Progman" ||
                    cls == "Windows.UI.Core.CoreWindow" ||
                    cls.StartsWith("Shell_"))
                    return false;

                // Get screen dimensions (primary monitor)
                int screenW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                int screenH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                // Get foreground window rect
                if (!GetWindowRect(fgWindow, out RECT rect)) return false;

                int winW = rect.Right - rect.Left;
                int winH = rect.Bottom - rect.Top;

                // Consider fullscreen if window covers entire screen
                bool coversScreen = winW >= screenW && winH >= screenH
                                    && rect.Left <= 0 && rect.Top <= 0;

                return coversScreen;
            }
            catch
            {
                return false;
            }
        }
    }
}