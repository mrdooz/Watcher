using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Watcher
{
    static class WatcherStructs
    {
        public const Int32 WM_COPYDATA = 0x4A;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WatchDataBase
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool add;
            public int token;
            public IntPtr hwnd;
        };

        // Send from the client when it wants us to watch something
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct WatchData
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool add;
            public int token;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string path;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string filter;
            public int flags;   // not used at the moment
        }

    }
}
