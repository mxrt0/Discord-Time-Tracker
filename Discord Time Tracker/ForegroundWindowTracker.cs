using System.Diagnostics;
using System.Text;

namespace Discord_Time_Tracker
{
    public class ForegroundWindowTracker : IDisposable
    {
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        public event EventHandler<ForegroundWindowChangedEventArgs>? ForegroundWindowChanged;

        private readonly WinApiHelper.WinEventDelegate _callback;
        private IntPtr _hook;
        public ForegroundWindowTracker()
        {
            _callback = WinEventProc;
            _hook = WinApiHelper.SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero,
                _callback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT);
        }

        private void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero || idObject != 0)
                return;

            if (!WinApiHelper.IsWindowVisible(hwnd))
                return;

            WinApiHelper.GetWindowThreadProcessId(hwnd, out uint pid);

            Process proc;
            try
            {
                proc = Process.GetProcessById((int)pid);
            }
            catch
            {
                return;
            }

            if (!proc.ProcessName.Equals("Discord", StringComparison.OrdinalIgnoreCase))
                return;

            string title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
                return;

            OnForegroundWindowChanged(hwnd, title);
        }

        private static string GetWindowTitle(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            WinApiHelper.GetWindowTextW(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }


        protected virtual void OnForegroundWindowChanged(IntPtr hwnd, string title)
        {
            ForegroundWindowChanged?.Invoke(
                this,
                new ForegroundWindowChangedEventArgs(hwnd, title)
            );
        }
        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                WinApiHelper.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
