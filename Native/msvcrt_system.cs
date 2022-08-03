using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable IDE1006 // Naming Styles
namespace IPvEVlanAutoServerEnterer.Native
{
    public static partial class Win32Functions
    {
        [DllImport("msvcrt.dll", SetLastError = true)]

        private static extern int system(string command);

        public static int System(string command)
        {
            if(string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Must provide command");

            var result = system(command);
            if(result == -1)
                throw new InvalidOperationException();

            return result;
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
