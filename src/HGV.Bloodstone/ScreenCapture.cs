using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Bloodstone
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        private static Process GetProcess()
        {

            // Cater for cases when the process can't be located.
            try
            {
                return Process.GetProcessesByName("dota2")[0];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new ApplicationException("Failed to find Dota process.", ex);
            }
        }

        public static Bitmap CaptureApplication()
        {
            Process proc = GetProcess();

            // You need to focus on the application
            SetForegroundWindow(proc.MainWindowHandle);
            ShowWindow(proc.MainWindowHandle, SW_RESTORE);

            // You need some amount of delay
            Thread.Sleep(10);

            var rect = new Rect();
            var error = IntPtr.Zero;

            // sometimes it gives an error, so try 3 times.
            for (int i = 0; i < 3; i++)
            {
                error = GetWindowRect(proc.MainWindowHandle, ref rect);
                if (error != IntPtr.Zero)
                    break;
            }

            if (error == IntPtr.Zero)
                throw new ApplicationException("Failed to get the windows bounds.");

            var width = rect.right - rect.left;
            var height = rect.bottom - rect.top;

            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Graphics.FromImage(bmp).CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            return bmp;
        }

    }
}
