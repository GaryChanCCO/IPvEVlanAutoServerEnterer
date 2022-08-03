using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        public static Bitmap PrintWindow(IntPtr windowHandle, bool clientOnly = false)
        {
            var windowRect = GetClientRect(windowHandle);
            var bmp = new Bitmap(windowRect.Width, windowRect.Height);
            using var gfxBmp = Graphics.FromImage(bmp);
            PrintWindow(windowHandle, gfxBmp.GetHdc(), clientOnly ? 1 : 0);
            gfxBmp.ReleaseHdc();
            return bmp;
        }


    }
}
