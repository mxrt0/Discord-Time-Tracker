using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace Discord_Time_Tracker
{
    internal class Program
    {
        private static readonly object _stateLock = new();
        private static readonly ManualResetEventSlim _exitEvent = new(false);

        private static readonly Dictionary<string, TimeSpan> _serverTimes = new();

        private static string? _currentServer;
        private static DateTime _serverStartedAt;
        private static bool _wasDiscordActive;

        [SupportedOSPlatform("Windows")]
        static void Main()
        {
            WinApiHelper.MessageBoxW(
                IntPtr.Zero,
                "Tracking Discord time.\nPress E to stop.",
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO)
            );

            uint hookThreadId = 0;

            Thread hookThread = new Thread(() =>
            {
                hookThreadId = WinApiHelper.GetCurrentThreadId();

                using var tracker = new ForegroundWindowTracker();
                tracker.ForegroundWindowChanged += OnForegroundWindowChanged;

                MSG msg;
                while (!_exitEvent.IsSet &&
                       WinApiHelper.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
                {
                    WinApiHelper.TranslateMessage(ref msg);
                    WinApiHelper.DispatchMessage(ref msg);
                }
            });

            hookThread.SetApartmentState(ApartmentState.STA);
            hookThread.Start();

            // Input thread
            Thread inputThread = new Thread(() =>
            {
                while (true)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.E)
                    {
                        _exitEvent.Set();
                        break;
                    }
                }
            });
            inputThread.Start();


            _exitEvent.Wait();

            WinApiHelper.PostThreadMessage(
                hookThreadId,
                WinApiHelper.WM_QUIT,
                UIntPtr.Zero,
                IntPtr.Zero
            );

            hookThread.Join();


            lock (_stateLock)
            {
                if (_currentServer != null)
                {
                    var elapsed = DateTime.UtcNow - _serverStartedAt;
                    AddTime(_currentServer, elapsed);
                    _currentServer = null;
                }
            }

            ShowResults();
        }
        private static void OnForegroundWindowChanged(object? sender, ForegroundWindowChangedEventArgs e)
        {
            lock (_stateLock)
            {
                bool isDiscordActive = IsDiscordActive();

                if (!isDiscordActive)
                {

                    if (_currentServer != null)
                    {
                        var elapsed = DateTime.UtcNow - _serverStartedAt;
                        AddTime(_currentServer, elapsed);
                        _currentServer = null;
                    }

                    _wasDiscordActive = false;
                    return;
                }

                string newServer = e.Title;

                if (!_wasDiscordActive)
                {

                    _currentServer = newServer;
                    _serverStartedAt = DateTime.UtcNow;
                    _wasDiscordActive = true;
                    return;
                }

                if (_currentServer != newServer)
                {

                    if (_currentServer != null)
                    {
                        var elapsed = DateTime.UtcNow - _serverStartedAt;
                        AddTime(_currentServer, elapsed);
                    }

                    _currentServer = newServer;
                    _serverStartedAt = DateTime.UtcNow;
                }
            }
        }

        private static void AddTime(string server, TimeSpan elapsed)
        {
            if (_serverTimes.TryGetValue(server, out var existing))
                _serverTimes[server] = existing + elapsed;
            else
                _serverTimes[server] = elapsed;
        }

        private static bool IsDiscordActive()
        {
            IntPtr hwnd = WinApiHelper.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            WinApiHelper.GetWindowThreadProcessId(hwnd, out uint pid);

            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName.Equals("Discord", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void ShowResults()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Time per server:\n");
            TimeSpan totalTime = TimeSpan.Zero;
            foreach (var kvp in _serverTimes.Where(kvp => kvp.Key.Contains("Discord", StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine($"{kvp.Key.Replace(" - Discord", "")} : {kvp.Value:hh\\:mm\\:ss}");
                totalTime += kvp.Value;
            }
            sb.AppendLine($"Total time spent: {totalTime:hh\\:mm\\:ss}");

            WinApiHelper.MessageBoxW(
                IntPtr.Zero,
                sb.ToString(),
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO)
            );
        }
    }
}
