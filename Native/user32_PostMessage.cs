using IPvEVlanAutoServerEnterer.Native.SystemDefinedMessages.General.KeyboardInputNotifications;
using IPvEVlanAutoServerEnterer.Native.SystemDefinedMessages.General.MouseInputNotifications;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr PostMessage(IntPtr handle, int Message, int wParam, int lParam);

        public static IntPtr PostMessage(IntPtr handle, ClientMouseButtonMessage message, short x, short y, int wParam = 0)
        {
            return PostMessage(handle, (int)message, wParam, (y * 0x10000) + x);
        }

        public static IntPtr PostMessage(IntPtr handle, KeyMessage message, VirtualKey virtualKey)
        {
            return PostMessage(handle, (int)message, (int)virtualKey, 0);
        }
    }
}
