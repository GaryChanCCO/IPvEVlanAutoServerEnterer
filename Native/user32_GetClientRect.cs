using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr handle, out Rect lpRect);

        public static Rectangle GetClientRect(IntPtr windowHandle)
        {
            if(GetClientRect(windowHandle, out var rect))
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            else
                throw new InvalidOperationException();
        }
    }
}
