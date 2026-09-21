using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab6.Object2;

internal static class NativeMethods
{
    public const int WM_COPYDATA = 0x004A;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
    public const uint SMTO_NORMAL = 0x0000;

    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        ref COPYDATASTRUCT lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    public static bool SendCopyData(IntPtr hWndDest, IntPtr hWndSrc, int dwData, string text, uint timeoutMs = 3000)
    {
        if (hWndDest == IntPtr.Zero) return false;

        GCHandle handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        try
        {
            var cds = new COPYDATASTRUCT
            {
                dwData = (IntPtr)dwData,
                cbData = text.Length * sizeof(char),
                lpData = handle.AddrOfPinnedObject()
            };

            IntPtr ret = SendMessageTimeout(
                hWndDest,
                WM_COPYDATA,
                hWndSrc,
                ref cds,
                SMTO_ABORTIFHUNG | SMTO_NORMAL,
                timeoutMs,
                out _);

            return ret != IntPtr.Zero;
        }
        finally
        {
            handle.Free();
        }
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


