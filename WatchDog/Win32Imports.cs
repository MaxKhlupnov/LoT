using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace HomeOS.Hub.Watchdog
{
    class Win32Imports
    {
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();
    }
}
