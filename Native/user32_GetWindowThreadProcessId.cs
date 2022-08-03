using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
