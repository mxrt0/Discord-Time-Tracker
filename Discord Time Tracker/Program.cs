namespace Discord_Time_Tracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WinApiHelper.MessageBoxW(IntPtr.Zero,
                "Your time spent on Discord is now being tracked!",
                "Discord Time Tracker",
                (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONINFO));
        }
    }
}
