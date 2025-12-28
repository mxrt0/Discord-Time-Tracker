using System.Runtime.InteropServices;
using System.Text;

namespace Discord_Time_Tracker
{
    public static class WinApiHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(
             IntPtr hWnd,
             string lpText,
             string lpCaption,
             uint uType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}
