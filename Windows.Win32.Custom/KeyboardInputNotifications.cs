namespace Windows.Win32.Custom
{
    internal enum KeyboardInputNotifications : uint
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
        WM_DEADCHAR = 0x0103,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSDEADCHAR = 0x0107,
        WM_UNICHAR = 0x0109,
    }
}