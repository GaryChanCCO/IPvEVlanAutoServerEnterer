using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.Custom;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;

using winmdroot = Windows.Win32;

Console.OutputEncoding = Console.InputEncoding = Encoding.Unicode;

var failCases = new HashSet<string>();
var successCases = new HashSet<string>();
short serverX;
short serverY;
var imageConverter = new ImageConverter();
var successCasesDir = Path.Combine(Environment.CurrentDirectory, "Success");
var failCasesDir = Path.Combine(Environment.CurrentDirectory, "Fail");

string GenerateFailFileName() => Path.Combine(failCasesDir, $"{Guid.NewGuid()}");
string GenerateSuccessFileName() => Path.Combine(successCasesDir, $"{Guid.NewGuid()}");
Rectangle GetClientRect(winmdroot.Foundation.HWND hWnd)
{
    if (PInvoke.GetClientRect(hWnd, out var rect)) return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    else throw new Win32Exception();
}
Bitmap PrintWindow(winmdroot.Foundation.HWND hWnd)
{
    var windowRect = GetClientRect(hWnd);
    var bmp = new Bitmap(windowRect.Width, windowRect.Height);
    using var gfxBmp = Graphics.FromImage(bmp);
    using var hdcHandle = new DangerousSafeHandle(gfxBmp.GetHdc());
    PInvoke.PrintWindow(hWnd, hdcHandle, winmdroot.Storage.Xps.PRINT_WINDOW_FLAGS.PW_CLIENTONLY);
    gfxBmp.ReleaseHdc();
    return bmp;
}
Bitmap CropToTargetServer(Bitmap origin)
{
    var result = new Bitmap(55, 16);
    using var g = Graphics.FromImage(result);
    g.DrawImage(origin,
                new Rectangle(0, 0, result.Width, result.Height),
                new Rectangle(serverX + 13, serverY + 86, result.Width, result.Height),
                GraphicsUnit.Pixel);
    return result;
}
string Image2Str(Image img) => (imageConverter.ConvertTo(img, typeof(byte[])) as byte[]).Select(b => b.ToString()).Aggregate((s1, s2) => s1 + s2);

string GetClassName(winmdroot.Foundation.HWND hWnd)
{
    var buffer = ArrayPool<char>.Shared.Rent(2048);
    unsafe
    {
        fixed (char* bufferPtr = buffer)
        {
            if (0 == PInvoke.GetClassName(hWnd, bufferPtr, buffer.Length)) throw new Win32Exception();
        }
    }
    return new string(buffer);
}

try
{
    const string configFile = "AppConfig.ini";

    #region Load Configurations.ini

    {
        var configDict = new Dictionary<string, string>();
        if (!File.Exists(configFile))
        {
            configDict.Add("X", "251");
            configDict.Add("Y", "156");
            File.WriteAllLines(configFile, configDict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
        else
        {
            foreach (var config in File.ReadAllLines(configFile))
            {
                var kvp = config.Split('=');
                if (kvp.Length != 2) throw new Exception(config);
                try
                {
                    configDict.Add(kvp[0], kvp[1]);
                }
                catch (ArgumentException e)
                {
                    throw new Exception(config, e);
                }
            }
        }
        serverX = short.Parse(configDict["X"]);
        serverY = short.Parse(configDict["Y"]);
    }

    #endregion Load Configurations.ini

    #region Load Cases

    {
        Directory.CreateDirectory(failCasesDir);
        foreach (var file in Directory.GetFiles(failCasesDir))
            if (Path.GetExtension(file) == ".txt") failCases.Add(File.ReadAllText(file));

        Directory.CreateDirectory(successCasesDir);
        foreach (var file in Directory.GetFiles(successCasesDir))
            if (Path.GetExtension(file) == ".txt") successCases.Add(File.ReadAllText(file));
    }

    #endregion Load Cases

    #region Check Admin Permission

    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) throw new Exception("Not admin.");
    }

    #endregion Check Admin Permission

    #region Get vlan Process

    var vlanProcess = Process.GetProcessesByName("vlan").FirstOrDefault();
    if (vlanProcess == default) throw new Exception("You must start vlan first.");

    #endregion Get vlan Process

    #region Check vlan window existence

    {
        var vlanRect = GetClientRect((winmdroot.Foundation.HWND)vlanProcess.MainWindowHandle);
        if (vlanRect.Size == Size.Empty) throw new Exception("You cannot hide vlan to system tray.");
    }

    #endregion Check vlan window existence

    IntPtr categoryComboHandle = default;
    {
        PInvoke.EnumChildWindows(
            (winmdroot.Foundation.HWND)vlanProcess.MainWindowHandle,
            new winmdroot.UI.WindowsAndMessaging.WNDENUMPROC((childHandle, _) =>
                {
                    if (!GetClassName(childHandle).Contains("WTL_TreeComboBox")) return true;
                    categoryComboHandle = childHandle;
                    return false;
                }),
            0);
        if (categoryComboHandle == default) throw new Exception("Connot find Category Combo.");
    }

    winmdroot.Foundation.HWND categoryListHandle = default;
    {
        PInvoke.EnumChildWindows(
            winmdroot.Foundation.HWND.Null,
            new winmdroot.UI.WindowsAndMessaging.WNDENUMPROC((childHandle, _) =>
                {
                    var owner = PInvoke.GetWindow(childHandle, winmdroot.UI.WindowsAndMessaging.GET_WINDOW_CMD.GW_OWNER);
                    if (owner != vlanProcess.MainWindowHandle) return true;
                    if (!GetClassName(childHandle).Contains("ATL:SysTreeView32")) return true;
                    categoryListHandle = childHandle;
                    return false;
                }),
            0);
        if (categoryListHandle == default) throw new Exception("Connot find Category List.");
    }

    winmdroot.Foundation.HWND serverListHandle = default;
    {
        PInvoke.EnumChildWindows(
            (winmdroot.Foundation.HWND)vlanProcess.MainWindowHandle,
            new winmdroot.UI.WindowsAndMessaging.WNDENUMPROC((childHandle, _) =>
                {
                    if (!GetClassName(childHandle).Contains("WTL_SortListViewCtrl")) return true;
                    if (PInvoke.GetWindowLong(childHandle, winmdroot.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE) != 0x5000000D) return true;
                    serverListHandle = childHandle;
                    return false;
                }),
            0);

        if (serverListHandle == default) throw new Exception("Cannot find Server List View.");
    }

    while (true)
    {
        if (!PInvoke.PostMessage(categoryListHandle, (uint)winmdroot.Custom.KeyboardInputNotifications.WM_KEYDOWN, (uint)winmdroot.Custom.VirtualKey.VK_UP, 0)) throw new Win32Exception();
        if (!PInvoke.PostMessage(categoryListHandle, (uint)winmdroot.Custom.KeyboardInputNotifications.WM_KEYDOWN, (uint)winmdroot.Custom.VirtualKey.VK_DOWN, 0)) throw new Win32Exception();
        Task.Delay(300).Wait();

        using var vlanBmp = PrintWindow((winmdroot.Foundation.HWND)vlanProcess.MainWindowHandle);
        using var serverBmp = CropToTargetServer(vlanBmp);
        var bmpStr = Image2Str(serverBmp);
        if (failCases.Contains(bmpStr) && !successCases.Contains(bmpStr)) goto LoopEnd;

        {
            winmdroot.Foundation.WPARAM X = (uint)(serverX + 8);
            winmdroot.Foundation.LPARAM Y = (serverY + 40);
            if (!PInvoke.PostMessage(serverListHandle, (uint)winmdroot.Custom.MouseInputNotifications.WM_LBUTTONDOWN, X, Y)) throw new Win32Exception();
            if (!PInvoke.PostMessage(serverListHandle, (uint)winmdroot.Custom.MouseInputNotifications.WM_LBUTTONDBLCLK, X, Y)) throw new Win32Exception();
        }
        Task.Delay(5500).Wait();

        bool isFailed = false;

        #region Check Fail Dialog

        {
            PInvoke.EnumChildWindows(
                winmdroot.Foundation.HWND.Null,
                new winmdroot.UI.WindowsAndMessaging.WNDENUMPROC((childHandle, _) =>
                {
                    unsafe
                    {
                        uint processId;
                        PInvoke.GetWindowThreadProcessId(childHandle, &processId);
                        if (processId != vlanProcess.Id) return true;
                    }

                    if (!GetClassName(childHandle).Contains("#32770"))
                        return true;

                    var okBtnHandle = PInvoke.GetWindow(childHandle, winmdroot.UI.WindowsAndMessaging.GET_WINDOW_CMD.GW_CHILD);
                    if (okBtnHandle == IntPtr.Zero) Console.WriteLine();

                    {
                        winmdroot.Foundation.WPARAM X = 35;
                        winmdroot.Foundation.LPARAM Y = 10;
                        if (!PInvoke.PostMessage(okBtnHandle, (uint)winmdroot.Custom.MouseInputNotifications.WM_LBUTTONDOWN, X, Y)) throw new Win32Exception();
                        if (!PInvoke.PostMessage(okBtnHandle, (uint)winmdroot.Custom.MouseInputNotifications.WM_LBUTTONUP, X, Y)) throw new Win32Exception();
                    }
                    isFailed = true;
                    return false;
                }),
                0);
        }

        #endregion Check Fail Dialog

        if (isFailed)
        {
            if (successCases.Contains(bmpStr)) goto LoopEnd;

            failCases.Add(bmpStr);
            var fileName = GenerateFailFileName();
            serverBmp.Save(fileName + ".png");
            File.WriteAllText(fileName + ".txt", bmpStr);
            goto LoopEnd;
        }

        if (!isFailed)
        {
            if (!successCases.Contains(bmpStr))
            {
                var fileName = GenerateSuccessFileName();
                serverBmp.Save(fileName + ".png");
                File.WriteAllText(fileName + ".txt", bmpStr);
            }
            break;
        }

    LoopEnd:
        Task.Delay(1000).Wait();
    }
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    Console.ReadKey();
}