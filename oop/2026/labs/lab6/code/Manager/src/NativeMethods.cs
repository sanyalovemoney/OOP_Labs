using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lab6.Manager
{
    // WM_COPYDATA — стандартне повідомлення Windows для передачі масивів
    // даних між вікнами різних процесів (методичка ООП, лаб. №6).
    // Дані копіюються системою в адресний простір процесу-приймача;
    // вказівник lpData дійсний лише під час обробки повідомлення.
    internal static class NativeMethods
    {
        public const int WM_COPYDATA = 0x004A;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;   // ідентифікатор типу даних (на власний розсуд)
            public int cbData;      // кількість байтів
            public IntPtr lpData;   // адреса даних
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        // Надсилання текстових даних вікну іншої програми.
        // wParam = hWnd вікна-відправника (приймач зможе відповісти).
        public static void SendCopyData(IntPtr hWndDest, IntPtr hWndSrc, int dwData, string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                COPYDATASTRUCT cds = new COPYDATASTRUCT
                {
                    dwData = new IntPtr(dwData),
                    cbData = bytes.Length,
                    lpData = buffer
                };
                SendMessage(hWndDest, WM_COPYDATA, hWndSrc, ref cds);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Розбір вхідного WM_COPYDATA (m.WParam = hWnd відправника)
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
