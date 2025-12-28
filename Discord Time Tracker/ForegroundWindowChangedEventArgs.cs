namespace Discord_Time_Tracker
{
    public class ForegroundWindowChangedEventArgs : EventArgs
    {
        public IntPtr Hwnd { get; }
        public string Title { get; }

        public ForegroundWindowChangedEventArgs(IntPtr hwnd, string title)
        {
            Hwnd = hwnd;
            Title = title;
        }
    }
}
