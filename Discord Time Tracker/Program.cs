using System.Diagnostics;

namespace Discord_Time_Tracker
{
    internal class Program
    {
        private static readonly object _stateLock = new();
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            TimeSpan timeSpent = TimeSpan.Zero;
            bool wasDiscordActive = false;
            bool exitRequested = false;

            WinApiHelper.MessageBoxW(IntPtr.Zero,
                "Your time spent on Discord is now being tracked! Press E to stop tracking and end program.",
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO));

            uint hookThreadId = 0;
            Thread hookThread = new Thread(() =>
            {
                hookThreadId = WinApiHelper.GetCurrentThreadId();
                using var windowTracker = new ForegroundWindowTracker();
                windowTracker.ForegroundWindowChanged += (_, e) =>
                {
                    bool isDiscordActive = IsDiscordActive();

                    lock (_stateLock)
                    {
                        if (isDiscordActive && !wasDiscordActive)
                        {
                            timer.Start();
                        }
                        else if (!isDiscordActive && wasDiscordActive)
                        {
                            timer.Stop();
                            timeSpent += timer.Elapsed;
                            timer.Reset();
                        }

                        wasDiscordActive = isDiscordActive;
                    }
                };

                MSG msg;
                while (!exitRequested && WinApiHelper.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
                {
                    WinApiHelper.TranslateMessage(ref msg);
                    WinApiHelper.DispatchMessage(ref msg);
                }
            });
            if (OperatingSystem.IsWindows())
            {
                hookThread.SetApartmentState(ApartmentState.STA);
            }
            hookThread.Start();

            Thread inputThread = new Thread(() =>
            {
                while (!exitRequested)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.E)
                        exitRequested = true;
                    Thread.Sleep(50);
                }
            });
            inputThread.Start();

            inputThread.Join();
            WinApiHelper.PostThreadMessage(hookThreadId, WinApiHelper.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
            hookThread.Join();

            if (timer.IsRunning)
            {
                lock (_stateLock)
                {
                    timer.Stop();
                    timeSpent += timer.Elapsed;
                    timer.Reset();
                }
            }

            WinApiHelper.MessageBoxW(
                IntPtr.Zero,
                $"Total time spent on Discord: {timeSpent:hh\\:mm\\:ss}",
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO)
            );
        }

        static bool IsDiscordActive()
        {
            IntPtr openWindowHandle = WinApiHelper.GetForegroundWindow();
            if (openWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            uint processId;
            WinApiHelper.GetWindowThreadProcessId(openWindowHandle, out processId);

            Process activeProcess = Process.GetProcessById((int)processId);
            return activeProcess.ProcessName.Equals("Discord", StringComparison.OrdinalIgnoreCase);
        }
    }
}
