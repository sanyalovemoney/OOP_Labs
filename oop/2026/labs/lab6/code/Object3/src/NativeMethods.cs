using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab6.Object3;

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
        if (m.LParam == IntPtr.Zero) return (IntPtr.Zero, string.Empty);
        var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
        if (cds.cbData <= 0 || cds.lpData == IntPtr.Zero)
            return (cds.dwData, string.Empty);

        string text = Marshal.PtrToStringUni(cds.lpData, cds.cbData / sizeof(char)) ?? string.Empty;
        return (cds.dwData, text);
    }
}

