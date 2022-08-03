using IPvEVlanAutoServerEnterer.Native;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using static System.Console;
using IPvEVlanAutoServerEnterer.Native.SystemDefinedMessages.General.MouseInputNotifications;
using IPvEVlanAutoServerEnterer.Native.SystemDefinedMessages.General.KeyboardInputNotifications;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Principal;
using IPvEVlanAutoServerEnterer.Exceptions;


namespace IPvEVlanAutoServerEnterer
{

    internal static class Program
    {
        private const string configFile = "AppConfig.ini";
        private static readonly string successCasesDir = Path.Combine(Environment.CurrentDirectory, "Success");
        private static readonly string failCasesDir = Path.Combine(Environment.CurrentDirectory, "Fail");
        static Program()
        {
            OutputEncoding = InputEncoding = Encoding.Unicode;

            //Load Configurations.ini
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
                        var kvp = config.Split("=");
                        if (kvp.Length != 2) throw new InvalidConfigurationException(config);
                        try
                        {
                            configDict.Add(kvp[0], kvp[1]);
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidConfigurationException(config, e);
                        }
                    }
                }
                serverX = short.Parse(configDict["X"]);
                serverY = short.Parse(configDict["Y"]);
            }

            //Load Cases
            {
                Directory.CreateDirectory(failCasesDir);
                foreach (var file in Directory.GetFiles(failCasesDir))
                {
                    if (Path.GetExtension(file) == ".txt")
                    {
                        failCases.Add(File.ReadAllText(file));
                    }
                }

                Directory.CreateDirectory(successCasesDir);
                foreach (var file in Directory.GetFiles(successCasesDir))
                {
                    if (Path.GetExtension(file) == ".txt")
                    {
                        successCases.Add(File.ReadAllText(file));
                    }
                }
            }
        }

        private static readonly HashSet<string> failCases = new HashSet<string>();
        private static readonly HashSet<string> successCases = new HashSet<string>();
        private static readonly short serverX;
        private static readonly short serverY;

        private static void Main(string[] args)
        {
            Execute();
        }

        private static void Execute()
        {
            //Check Admin Permission
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    WriteLine("Not admin.");
                    goto End;
                }
            }


            //Get vlan Process
            var vlanProcess = Process.GetProcessesByName("vlan").FirstOrDefault();
            if (vlanProcess == default)
            {
                WriteLine("You must start vlan first.");
                goto End;
            }

            //Check vlan window existence
            {
                var vlanRect = Win32Functions.GetClientRect(vlanProcess.MainWindowHandle);

                if (vlanRect.Size == Size.Empty)
                {
                    WriteLine("You cannot hide vlan to system tray.");
                    goto End;
                }
            }

            IntPtr categoryComboHandle = default;
            {
                bool EnumChildProc(IntPtr childHandle, IntPtr _)
                {
                    var classNameBuilder = new StringBuilder();
                    Win32Functions.GetClassName(childHandle, classNameBuilder, 2048);
                    if (!classNameBuilder.ToString().Contains("WTL_TreeComboBox"))
                        return true;

                    categoryComboHandle = childHandle;
                    return false;
                }

                Win32Functions.EnumChildWindows(vlanProcess.MainWindowHandle,
                                                new Win32Functions.EnumChildProc(EnumChildProc),
                                                0);

                if (categoryComboHandle == default)
                {
                    WriteLine("Connot find Category Combo.");
                    goto End;
                }
            }

            IntPtr categoryListHandle = default;
            {
                Win32Functions.PostMessage(new IntPtr(0x000207D8), ClientMouseButtonMessage.WM_LBUTTONDOWN, 85, 12);
                Win32Functions.EnumChildWindows(IntPtr.Zero,
                                new Win32Functions.EnumChildProc((childHandle, lParam) =>
                                {
                                    var owner = Win32Functions.GetWindow(childHandle, Win32Functions.GetWindowType.GW_OWNER);
                                    if (owner != vlanProcess.MainWindowHandle)
                                        return true;

                                    var classNameBuilder = new StringBuilder();
                                    Win32Functions.GetClassName(childHandle, classNameBuilder, 2048);
                                    if (!classNameBuilder.ToString().Contains("ATL:SysTreeView32"))
                                        return true;

                                    categoryListHandle = childHandle;
                                    return false;
                                }),
                                0);
                if (categoryListHandle == default)
                {
                    WriteLine("Connot find Category List.");
                    goto End;
                }
            }

            IntPtr serverListHandle = default;
            {
                bool EnumChildProc(IntPtr childHandle, IntPtr _)
                {
                    var classNameBuilder = new StringBuilder();
                    Win32Functions.GetClassName(childHandle, classNameBuilder, 2048);
                    if (!classNameBuilder.ToString().Contains("WTL_SortListViewCtrl"))
                        return true;

                    if (Win32Functions.GetWindowLong(childHandle, Win32Functions.WindowLongField.GWL_STYLE) != 0x5000000D)
                        return true;
                    serverListHandle = childHandle;
                    return false;
                }

                Win32Functions.EnumChildWindows(vlanProcess.MainWindowHandle,
                                                new Win32Functions.EnumChildProc(EnumChildProc),
                                                0);

                if (serverListHandle == default)
                {
                    WriteLine("Connot find Server List View.");
                    goto End;
                }
            }

            while (true)
            {
                Win32Functions.PostMessage(categoryListHandle, KeyMessage.WM_KEYDOWN, VirtualKey.VK_UP);
                Win32Functions.PostMessage(categoryListHandle, KeyMessage.WM_KEYDOWN, VirtualKey.VK_DOWN);
                Task.Delay(300).Wait();
                using var vlanBmp = Win32Functions.PrintWindow(vlanProcess.MainWindowHandle, true);
                using var serverBmp = CropToTargetServer(vlanBmp);
                var bmpStr = Image2Str(serverBmp);
                if (failCases.Contains(bmpStr) && !successCases.Contains(bmpStr))
                    goto LoopEnd;

                {
                    var X = (short)(serverX + 30);
                    var Y = (short)(serverY + 7);
                    Win32Functions.PostMessage(serverListHandle, ClientMouseButtonMessage.WM_LBUTTONDOWN, X, Y);
                    Win32Functions.PostMessage(serverListHandle, ClientMouseButtonMessage.WM_LBUTTONDBLCLK, X, Y);
                }
                Task.Delay(5500).Wait();

                //Check Fail Dialog
                bool isFailed = false;
                {
                    bool EnumChildProc(IntPtr childHandle, IntPtr lParam)
                    {
                        Win32Functions.GetWindowThreadProcessId(childHandle, out var processId);

                        if (processId != vlanProcess.Id)
                            return true;

                        var classNameBuilder = new StringBuilder();
                        Win32Functions.GetClassName(childHandle, classNameBuilder, 2048);
                        if (!classNameBuilder.ToString().Contains("#32770"))
                            return true;

                        var okBtnHandle = Win32Functions.GetWindow(childHandle, Win32Functions.GetWindowType.GW_CHILD);

                        {
                            const short X = 35;
                            const short Y = 10;
                            Win32Functions.PostMessage(okBtnHandle, ClientMouseButtonMessage.WM_LBUTTONDOWN, X, Y);
                            Win32Functions.PostMessage(okBtnHandle, ClientMouseButtonMessage.WM_LBUTTONUP, X, Y);
                        }
                        isFailed = true;
                        return false;
                    }

                    Win32Functions.EnumChildWindows(IntPtr.Zero,
                                                    new Win32Functions.EnumChildProc(new Win32Functions.EnumChildProc(EnumChildProc)),
                                                    0);
                }

                if (isFailed)
                {
                    if (successCases.Contains(bmpStr))
                        goto LoopEnd;

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

        End:
            Win32Functions.System("Pause");
        }

        private static string GenerateFailFileName() => Path.Combine(failCasesDir, $"{Guid.NewGuid()}");

        private static string GenerateSuccessFileName() => Path.Combine(successCasesDir, $"{Guid.NewGuid()}");

        private static Bitmap CropToTargetServer(Bitmap origin)
        {
            var result = new Bitmap(55, 16);
            using var g = Graphics.FromImage(result);
            g.DrawImage(origin,
                        new Rectangle(0, 0, result.Width, result.Height),
                        new Rectangle(serverX + 13, serverY + 86, result.Width, result.Height),
                        GraphicsUnit.Pixel);
            return result;
        }

        private static readonly ImageConverter imageConverter = new ImageConverter();
        private static string Image2Str(Image img)
        {
            return imageConverter.ConvertTo<byte[]>(img).Select(b => b.ToString()).Aggregate((s1, s2) => s1 + s2);
        }
    }
}
