using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    /// <summary>
    /// Registers a truly system-wide keyboard shortcut using a dedicated
    /// message-only window on its own thread. This ensures the hotkey
    /// works regardless of which app/window is currently focused.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        // ── Win32 API ─────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        // ── Constants ─────────────────────────────────────────────
        public const uint MOD_ALT     = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT   = 0x0004;
        public const uint MOD_WIN     = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        private const int WM_HOTKEY = 0x0312;
        private const uint WM_QUIT   = 0x0012;

        // ── Instance fields ───────────────────────────────────────
        private readonly int _hotkeyId;
        private Thread? _messageThread;
        private uint _messageThreadId;
        private volatile bool _running;
        private ManualResetEventSlim _registrationDone = new(false);
        private bool _registrationSuccess;

        private uint _modifiers;
        private uint _vk;

        public event Action? HotkeyPressed;

        public GlobalHotkeyService(int hotkeyId = 9001)
        {
            _hotkeyId = hotkeyId;
        }

        /// <summary>
        /// Registers the hotkey. Creates a dedicated background thread
        /// with its own message loop for reliable global hotkey handling.
        /// </summary>
        public bool Register(uint modifiers, uint virtualKey)
        {
            if (_running) return false;

            _modifiers = modifiers;
            _vk = virtualKey;
            _running = true;
            _registrationDone.Reset();

            _messageThread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "GlobalHotkeyThread"
            };
            _messageThread.SetApartmentState(ApartmentState.STA);
            _messageThread.Start();

            // Wait up to 2 seconds for registration to complete
            _registrationDone.Wait(TimeSpan.FromSeconds(2));
            return _registrationSuccess;
        }

        private void MessageLoop()
        {
            _messageThreadId = GetCurrentThreadId();

            // Register hotkey with hWnd = IntPtr.Zero
            // When hWnd is 0, WM_HOTKEY is posted to the calling THREAD's queue
            _registrationSuccess = RegisterHotKey(IntPtr.Zero, _hotkeyId,
                _modifiers | MOD_NOREPEAT, _vk);

            _registrationDone.Set();

            if (!_registrationSuccess)
            {
                _running = false;
                return;
            }

            // Thread message loop
            while (_running && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == _hotkeyId)
                {
                    try
                    {
                        // Marshal event back to UI thread
                        var app = System.Windows.Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HotkeyPressed?.Invoke();
                            }));
                        }
                    }
                    catch { }
                }
                else if (msg.message == WM_QUIT)
                {
                    break;
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            // Cleanup
            UnregisterHotKey(IntPtr.Zero, _hotkeyId);
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        public void Unregister()
        {
            if (!_running) return;
            _running = false;

            // Post WM_QUIT to break out of the message loop
            if (_messageThreadId != 0)
            {
                PostThreadMessage(_messageThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }

            _messageThread?.Join(TimeSpan.FromSeconds(1));
            _messageThread = null;
        }

        public void Dispose()
        {
            Unregister();
            _registrationDone.Dispose();
        }
    }
}