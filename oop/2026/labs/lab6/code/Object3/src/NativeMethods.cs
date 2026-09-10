using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab6.Object3
{
    // WM_COPYDATA — прийом команд від Manager (методичка, лаб. №6)
    internal static class NativeMethods
    {
        public const int WM_COPYDATA = 0x004A;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public static (IntPtr dwData, string text) ParseCopyData(Message m)
        {
            COPYDATASTRUCT cds = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
            string text = cds.cbData > 0
                ? Marshal.PtrToStringUni(cds.lpData, cds.cbData / 2) ?? ""
                : "";
            return (cds.dwData, text);
        }
    }
}
