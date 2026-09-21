<div style="text-align: center; font-size: 24px; margin-top: 60px;">

Міністерство освіти і науки України

Національний технічний університет України
«Київський політехнічний інститут імені Ігоря Сікорського»

Факультет інформатики та обчислювальної техніки
Кафедра обчислювальної техніки

</div>

<div style="text-align: center; margin-top: 120px;">

<h1 style="font-size: 22px;">Лабораторна робота №6</h1>

<h2 style="font-size: 22px;">з дисципліни «Об'єктно-орієнтоване програмування»</h2>

<h3 style="font-size: 22px; margin-top: 20px;">на тему</h3>

<h2 style="font-size: 22px;">«Побудування програмної системи з множини об'єктів, керованих повідомленнями»</h2>

</div>

<div style="text-align: right; margin-top: 120px; font-size: 18px;">

<strong>Виконав:</strong><br>
Мащута Олександр<br>
студент групи IM-051<br>
номер у списку групи: 7<br><br>

<strong>Перевірив:</strong><br>
Рекечинський Дмитро Олександрович

</div>

<div style="text-align: center; margin-top: 120px; font-size: 20px;">

Київ 2026

</div>

---

## Завдання

1. Розробити програмну систему із трьох незалежних додатків C# WinForms (`Manager`, `Object2`, `Object3`).
2. Реалізувати механізм міжпроцесної взаємодії (IPC) за допомогою повідомлень Windows `WM_COPYDATA`.
3. Використати системний буфер обміну (Windows Clipboard) для передачі масиву значень між процесами.
4. Забезпечити автоматичний запуск, пошук і закриття супутніх процесів при завершенні програми-менеджера.
5. Налагодити систему та перевірити точність обчислень та відображення графіка.
6. Оформити звіт.

---

## Завдання згідно варіанту

Для студента №7 (Ж = 7, Варіант = Ж mod 4 = 3):
1. **Manager (Lab6)**: Приймає параметри `n, Min, Max` від користувача, шукає/запускає `Object2` та `Object3`, передає параметри через `WM_COPYDATA`.
2. **Object2**: Генерує вектор з `n` дробових чисел (`double`) у діапазоні `Min`..`Max`, відображає числа у кілька стовпчиків і рядків, записує їх у системний Clipboard та відповідає Manager повідомленням `READY`.
3. **Object3**: За командою `READ_CLIPBOARD` зчитує дані з Clipboard і будує графік $y=f(x)$, де $y$ — значення елементів вектора, а $x$ — їхні індекси, з осями координат і числовими підписами.

---

## Вихідний текст програми

### Клас міжпроцесної взаємодії NativeMethods.cs

```csharp
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lab6.Manager
{
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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

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
```

### Програма Manager (ManagerForm.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab6.Manager
{
    public class ManagerForm : Form
    {
        internal const int MsgParams = 1;
        internal const int MsgReady = 2;
        internal const int MsgReadClipboard = 3;

        private TextBox txtN, txtMin, txtMax;
        private Button btnRun;
        private Label lblStatus;

        private readonly List<Process> _companions = new List<Process>();
        private IntPtr _object3HWnd = IntPtr.Zero;

        public ManagerForm()
        {
            this.Text = "Lab 6 - Manager";
            this.Size = new Size(340, 300);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(30, 40);

            // ... Налаштування полів введення n, Min, Max ...

            this.FormClosed += (s, e) => CloseCompanions();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_COPYDATA)
            {
                var (dwData, text) = NativeMethods.ParseCopyData(m);
                if (dwData.ToInt64() == MsgReady && _object3HWnd != IntPtr.Zero)
                {
                    NativeMethods.SendCopyData(_object3HWnd, this.Handle, MsgReadClipboard, text);
                    lblStatus.Text = $"READY від Object2 (значень: {text}). Object3 будує графік y=f(x).";
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void CloseCompanions()
        {
            foreach (var p in _companions)
            {
                try
                {
                    p.Refresh();
                    if (!p.HasExited) p.CloseMainWindow();
                }
                catch { }
            }
        }
    }
}
```

### Генератор даних Object2 (Object2Form.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object2
{
    public class Object2Form : Form
    {
        private const int MsgParams = 1;
        private const int MsgReady = 2;

        private readonly List<double> _values = new List<double>();
        private IntPtr _managerHWnd = IntPtr.Zero;

        private void OnParamsReceived(string text)
        {
            string[] p = text.Split(';');
            if (p.Length < 3 || !int.TryParse(p[0], out int n) ||
                !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double min) ||
                !double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double max)) return;

            Random rnd = new Random();
            _values.Clear();
            for (int i = 0; i < n; i++)
            {
                _values.Add(min + rnd.NextDouble() * (max - min));
            }

            var parts = new string[n];
            for (int i = 0; i < n; i++)
            {
                parts[i] = _values[i].ToString("F4", CultureInfo.InvariantCulture);
            }

            Clipboard.SetText(string.Join(";", parts));
            this.Invalidate();

            if (_managerHWnd != IntPtr.Zero)
            {
                this.BeginInvoke(new Action(() =>
                    NativeMethods.SendCopyData(_managerHWnd, this.Handle, MsgReady, _values.Count.ToString())));
            }
        }
    }
}
```

### Побудовник графіка Object3 (Object3Form.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object3
{
    public class Object3Form : Form
    {
        private readonly List<double> _values = new List<double>();

        private void TryReadClipboard()
        {
            string data = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(data)) return;

            var parsed = new List<double>();
            foreach (var s in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                    parsed.Add(v);
            }

            _values.Clear();
            _values.AddRange(parsed);
            this.Invalidate();
        }
    }
}
```

---

## Діаграми

### Діаграма компонентів та передачі даних

```
┌──────────────┐  WM_COPYDATA (dwData=1: n,Min,Max)   ┌──────────────┐
│   Manager    │─────────────────────────────────────▶│   Object2    │
│   (Lab6)     │                                      │ (Генерація   │
│              │◀─────────────────────────────────────│  вектора     │
│              │  WM_COPYDATA (dwData=2: READY)       │  → Clipboard)│
│              │                                      └──────────────┘
│              │  WM_COPYDATA (dwData=3: READ)        ┌──────────────┐
│              │─────────────────────────────────────▶│   Object3    │
└──────────────┘                                      │ (Читання     │
       │ Закриття Manager                             │ Clipboard →  │
       └─────────────────────────────────────────────▶│  графік y=f(x)│
                                                      └──────────────┘
```

---

## Скріншоти

### Три вікна системи в роботі
<img src="../screenshots/three_windows.png" style="width: 100%; max-width: 800px;">
_Рис. 1. Одночасна робота трьох процесів: Manager, Object2 та Object3_

---

### Графік y=f(x) в Object3
<img src="../screenshots/object3_graph.png" style="width: 100%; max-width: 800px;">
_Рис. 2. Відображення графіка з осями та підписами координат в Object3_

---

## Висновки

У лабораторній роботі №6 було розроблено розподілену програмну систему із трьох C# WinForms додатків, що взаємодіють між собою за допомогою обміну повідомленнями `WM_COPYDATA` та системного буфера обміну `Clipboard`.

Програма `Manager` автоматично контролює життєвий цикл супутніх процесів, передає вхідні параметри `Object2`, а після завершення генерації даних повідомляє `Object3` про необхідність побудови графіка $y=f(x)$. Систему повністю налагоджено, виключено зациклення `SendMessage` та забезпечено автоматичне завершення всіх вікон при виході з головної програми.
