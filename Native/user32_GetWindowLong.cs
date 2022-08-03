using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        public enum WindowLongField
        {
            GWL_USERDATA = -21,
            GWL_EXSTYLE = -20,
            GWL_STYLE = -16,
            GWL_ID = -12,
            GWL_HWNDPARENT = -8,
            GWL_HINSTANCE = -6,
            GWL_WNDPROC = -4
        }

        [DllImport("user32.dll")]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        public static long GetWindowLong(IntPtr handle, WindowLongField field)
        {
            return GetWindowLong(handle, (int)field);
        }
    }
}
