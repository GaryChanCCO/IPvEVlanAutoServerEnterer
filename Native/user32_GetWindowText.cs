using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    }
}
