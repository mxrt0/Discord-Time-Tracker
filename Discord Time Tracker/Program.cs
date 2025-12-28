using System.Diagnostics;

namespace Discord_Time_Tracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            TimeSpan timeSpent = TimeSpan.Zero;
            using var windowTracker = new ForegroundWindowTracker();

            WinApiHelper.MessageBoxW(IntPtr.Zero,
                "Your time spent on Discord is now being tracked!",
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO));


            windowTracker.ForegroundWindowChanged += (_, e) =>
            {

            };
        }

        bool IsDiscordActive()
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
