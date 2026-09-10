# Лабораторна робота №6: Побудування програмної системи з множини об'єктів, керованих повідомленнями

## Опис
Ця робота є завершальною в курсі ООП. Головною метою було створення програмної системи, що складається з трьох незалежних додатків, які взаємодіють між собою за допомогою **обміну повідомленнями Windows (WM_COPYDATA)** та системного буфера обміну (Clipboard).

**Варіант 3** (Ж = 7, номер варіанту = Ж mod 4 = 3) згідно таблиці 6.1 методички:
- **Lab6 (Manager)**: користувач вводить значення `n, Min, Max`; програма викликає Object2, Object3 і виконує обмін повідомленнями з ними для передавання/отримання інформації.
- **Object2**: створює вектор `n` дробових (double) чисел у діапазоні Min–Max; показує числові значення у декількох стовпчиках та рядках у власному головному вікні; записує дані в Clipboard Windows у текстовому форматі.
- **Object3**: зчитує дані з Clipboard Windows; відображає графік y=f(x), де y — значення вектора, x — індекси елементів. Графік, як у математиці — лінія, що проходить через точки (x,y) у порядку зростання x; осі координат з підписами числових значень x, y.

---

## Архітектура системи та алгоритм обміну повідомленнями

Усі три програми збираються у спільну папку (`bin\Debug`) — аналог спільної `\Debug` трьох проєктів одного Solution за методичкою. Три проєкти об'єднані у рішення `Lab6.slnx`.

Обмін виконується повідомленням **WM_COPYDATA** (передача масивів даних між вікнами різних процесів через `SendMessage` + структура `COPYDATASTRUCT`). Поле `dwData` ідентифікує тип повідомлення (рекомендація методички):

```
┌──────────────┐  WM_COPYDATA (dwData=1: "n;Min;Max")  ┌──────────────┐
│   Manager    │──────────────────────────────────────▶│   Object2    │
│   (Lab6)     │                                       │  генерує     │
│  FindOrStart │◀──────────────────────────────────────│  вектор      │
│  (вже запущ. │  WM_COPYDATA (dwData=2: "READY")      │  double      │
│   — знайде,  │                                       │  → Clipboard │
│  не дублює)  │                                       └──────────────┘
│              │  WM_COPYDATA (dwData=3: "READ")       ┌──────────────┐
│              │──────────────────────────────────────▶│   Object3    │
│              │                                       │ читає        │
└──────────────┘                                       │ Clipboard →  │
       │                                               │ графік y=f(x)│
       │ закриття Manager → CloseMainWindow()          └──────────────┘
       └──────────────────────────────────────────────▶ Object2, Object3 завершуються
```

Послідовність (алгоритм рис. 6.3/6.4 методички):
1. Користувач вводить `n, Min, Max` і натискає «Виконати» — далі все автоматично.
2. Manager знаходить вже запущені Object2/Object3 (`Process.GetProcessesByName`) або запускає їх (`Process.Start`), чекає на створення головних вікон.
3. Manager надсилає Object2 параметри через **WM_COPYDATA** (dwData=1). `wParam` = hWnd Manager — щоб Object2 знав, кому відповідати.
4. Object2 генерує вектор, показує його у стовпчиках/рядках, записує у Clipboard (текст, InvariantCulture) і надсилає Manager відповідь **READY** (dwData=2) через `BeginInvoke` — без перехресного блокування `SendMessage`.
5. Manager, отримавши READY, надсилає Object3 команду **READ_CLIPBOARD** (dwData=3).
6. Object3 читає Clipboard, будує графік y=f(x) з осями та підписами.
7. При закритті Manager автоматично закриває Object2 і Object3 (`CloseMainWindow`).

---

## Відповідність вимогам методички щодо організації системи

| # | Вимога | Реалізація |
|---|--------|-----------|
| 1 | Користувач вводить параметри і натискає «Так/Виконати» — далі тільки спостерігає | Форма Manager з полями n/Min/Max + кнопка «Виконати»; після кліка весь ланцюжок автоматичний |
| 2 | Обмін повідомленнями автоматично; Lab6 сама знаходить та викликає Object2/Object3 | `FindOrStart()` + ланцюжок WM_COPYDATA (1→2→3) без участі користувача |
| 3 | Вікна Object2/Object3 автоматично розташовані так, щоб усі результати було видно; Lab6 залишається активною | `StartPosition=Manual`: Manager (30,40), Object2 (400,40), Object3 (870,40) — поруч, не перекриваються; Manager не закривається |
| 4 | Робота у випадках, коли Object2/Object3 уже були викликані | `Process.GetProcessesByName` — повторне використання запущеного екземпляра; повторний клік «Виконати» просто надсилає нові параметри |
| 5 | Після завершення Lab6 завершуються Object2 і Object3 | `FormClosed → CloseCompanions()` — `CloseMainWindow()` для всіх відстежених процесів |

Додатково: валідація введення (n — ціле 2..1000, Min < Max — дробові); дані у Clipboard — лише вектор значень (параметри йдуть через WM_COPYDATA, тому конфлікт «параметри vs точки» неможливий); Object3 ігнорує чужі/пошкоджені дані буфера (зберігає попередній стан); доступ до Clipboard обгорнутий у try/catch (CLIPBRD_E_CANT_OPEN).

---

## Вихідний код

### Manager/src/NativeMethods.cs (WM_COPYDATA; аналогічні класи є в Object2 та Object3)
```csharp
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lab6.Manager
{
    // WM_COPYDATA — стандартне повідомлення Windows для передачі масивів
    // даних між вікнами різних процесів (методичка ООП, лаб. №6).
    internal static class NativeMethods
    {
        public const int WM_COPYDATA = 0x004A;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;   // ідентифікатор типу даних
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
```

### Manager/src/MainWindow.cs
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
    // Програма-менеджер (Object1 за методичкою): збирає параметри у діалозі,
    // автоматично знаходить/запускає Object2 та Object3 і керує обміном
    // повідомленнями WM_COPYDATA без участі користувача.
    public class ManagerForm : Form
    {
        // Коди повідомлень (поле dwData) — однакові в усіх трьох програмах
        internal const int MsgParams = 1;        // Manager -> Object2
        internal const int MsgReady = 2;         // Object2 -> Manager
        internal const int MsgReadClipboard = 3; // Manager -> Object3

        private TextBox txtN, txtMin, txtMax;
        private Button btnRun;
        private Label lblStatus;
        private int _yPos = 20; // явний лічильник позицій рядків

        private readonly List<Process> _companions = new List<Process>();
        private IntPtr _object3HWnd = IntPtr.Zero;

        public ManagerForm()
        {
            this.Text = "Lab 6 - Manager";
            this.Size = new Size(340, 300);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(30, 40); // вікна розставлені так, щоб
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // усі результати були видні
            this.MaximizeBox = false;

            AddInput("n (2..1000):", txtN = new TextBox { Text = "10" });
            AddInput("Min:", txtMin = new TextBox { Text = "0" });
            AddInput("Max:", txtMax = new TextBox { Text = "100" });

            btnRun = new Button { Text = "Виконати", Location = new Point(110, _yPos + 10), Size = new Size(110, 32) };
            btnRun.Click += OnRunClicked;
            this.Controls.Add(btnRun);

            lblStatus = new Label { Text = "Готовий до запуску системи.", Location = new Point(20, _yPos + 55), Size = new Size(295, 50) };
            this.Controls.Add(lblStatus);

            // Вимога 5: після завершення Manager завершуються Object2/Object3
            this.FormClosed += (s, e) => CloseCompanions();
        }

        private void AddInput(string label, TextBox tb)
        {
            Label lbl = new Label { Text = label, Location = new Point(20, _yPos), AutoSize = true };
            tb.Location = new Point(130, _yPos - 3);
            tb.Size = new Size(100, 20);
            this.Controls.Add(lbl);
            this.Controls.Add(tb);
            _yPos += 34;
        }

        private async void OnRunClicked(object sender, EventArgs e)
        {
            // Валідація параметрів згідно варіанту 3: n, Min, Max (дробові)
            if (!int.TryParse(txtN.Text, out int n) || n < 2 || n > 1000)
            {
                MessageBox.Show("Кількість значень n: ціле число від 2 до 1000.",
                    "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!double.TryParse(txtMin.Text, out double min) ||
                !double.TryParse(txtMax.Text, out double max) || min >= max)
            {
                MessageBox.Show("Діапазон: числа Min < Max.",
                    "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRun.Enabled = false;
            lblStatus.Text = "Пошук/запуск Object2 та Object3...";
            try
            {
                // Вимога 4: якщо програми вже запущені — використовуємо їх
                Process p2 = FindOrStart("Object2");
                Process p3 = FindOrStart("Object3");

                IntPtr h2 = await WaitForWindowAsync(p2);
                IntPtr h3 = await WaitForWindowAsync(p3);
                if (h2 == IntPtr.Zero || h3 == IntPtr.Zero)
                {
                    lblStatus.Text = "Помилка: не вдалося знайти вікно Object2/Object3.";
                    return;
                }
                _object3HWnd = h3;

                // Параметри надсилаються повідомленням WM_COPYDATA
                string payload = string.Join(";",
                    n.ToString(CultureInfo.InvariantCulture),
                    min.ToString(CultureInfo.InvariantCulture),
                    max.ToString(CultureInfo.InvariantCulture));
                NativeMethods.SendCopyData(h2, this.Handle, MsgParams, payload);
                lblStatus.Text = $"Параметри \"{payload}\" надіслані Object2 (WM_COPYDATA). Очікування READY...";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Помилка: " + ex.Message;
            }
            finally
            {
                btnRun.Enabled = true;
            }
        }

        // Прийом повідомлень-відповідей від Object2
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_COPYDATA)
            {
                var (dwData, text) = NativeMethods.ParseCopyData(m);
                if (dwData.ToInt64() == MsgReady && _object3HWnd != IntPtr.Zero)
                {
                    // Object2 повідомив, що дані у Clipboard готові —
                    // даємо команду Object3 прочитати їх і побудувати графік
                    NativeMethods.SendCopyData(_object3HWnd, this.Handle, MsgReadClipboard, text);
                    lblStatus.Text = $"READY від Object2 (значень: {text}). Object3 будує графік y=f(x).";
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private Process FindOrStart(string name)
        {
            // Шукаємо вже запущений екземпляр із готовим головним вікном
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    p.Refresh();
                    if (!p.HasExited && p.MainWindowHandle != IntPtr.Zero)
                    {
                        TrackCompanion(p);
                        return p;
                    }
                }
                catch { /* процес недоступний — ігноруємо */ }
            }

            // Усі три exe лежать у спільній папці (спільний OutputPath проєктів)
            string exePath = Path.Combine(AppContext.BaseDirectory, name + ".exe");
            Process started = Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true })
                ?? throw new InvalidOperationException("Не вдалося запустити " + name);
            TrackCompanion(started);
            return started;
        }

        private void TrackCompanion(Process p)
        {
            if (!_companions.Contains(p)) _companions.Add(p);
        }

        private static async Task<IntPtr> WaitForWindowAsync(Process p, int timeoutMs = 10000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                p.Refresh();
                if (p.HasExited) return IntPtr.Zero;
                if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
                await Task.Delay(100);
            }
            return IntPtr.Zero;
        }

        private void CloseCompanions()
        {
            foreach (var p in _companions)
            {
                try
                {
                    p.Refresh();
                    if (!p.HasExited) p.CloseMainWindow(); // коректне закриття вікна
                }
                catch { /* процес уже недоступний */ }
            }
        }
    }
}
```

### Object2/src/MainWindow.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object2
{
    // Object2 (варіант 3, Ж = 7): отримує параметри n;Min;Max повідомленням
    // WM_COPYDATA від Manager, створює вектор із n дробових (double) чисел
    // у діапазоні Min–Max, показує значення у декількох стовпчиках та рядках,
    // записує дані у Clipboard у текстовому форматі та надсилає Manager
    // повідомлення-відповідь READY.
    public class Object2Form : Form
    {
        private const int MsgParams = 1; // Manager -> Object2
        private const int MsgReady = 2;  // Object2 -> Manager

        private readonly List<double> _values = new List<double>();
        private IntPtr _managerHWnd = IntPtr.Zero;
        private string _status = "Очікування параметрів (n;Min;Max) від Manager через WM_COPYDATA...";

        public Object2Form()
        {
            this.Text = "Object 2 - Data Gen";
            this.Size = new Size(440, 380);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(400, 40); // поряд із Manager — усі вікна видні
            this.DoubleBuffered = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_COPYDATA)
            {
                var (dwData, text) = NativeMethods.ParseCopyData(m);
                if (dwData.ToInt64() == MsgParams)
                {
                    _managerHWnd = m.WParam; // hWnd відправника — для відповіді
                    OnParamsReceived(text);
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void OnParamsReceived(string text)
        {
            string[] p = text.Split(';');
            if (p.Length < 3 ||
                !int.TryParse(p[0], out int n) || n < 2 || n > 1000 ||
                !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double min) ||
                !double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double max) ||
                min >= max)
            {
                _status = "Отримано некоректні параметри: \"" + text + "\"";
                this.Invalidate();
                return;
            }

            // Вектор із n дробових чисел у діапазоні Min–Max
            Random rnd = new Random();
            _values.Clear();
            for (int i = 0; i < n; i++)
            {
                _values.Add(min + rnd.NextDouble() * (max - min));
            }

            // Запис у Clipboard у текстовому форматі (згідно варіанту).
            // InvariantCulture: роздільник дробової частини — крапка,
            // щоб Object3 гарантовано розібрав дані незалежно від локалі.
            var parts = new string[n];
            for (int i = 0; i < n; i++)
            {
                parts[i] = _values[i].ToString("F4", CultureInfo.InvariantCulture);
            }

            try
            {
                Clipboard.SetText(string.Join(";", parts));
                _status = $"Вектор із {n} double-значень у діапазоні [{min}; {max}] створено. Дані записано у Clipboard.";
            }
            catch (Exception ex)
            {
                _status = "Помилка запису у Clipboard: " + ex.Message;
            }
            this.Invalidate();

            // Відповідь Manager надсилаємо асинхронно (BeginInvoke), щоб уникнути
            // перехресного блокування SendMessage під час обробки вхідного WM_COPYDATA
            if (_managerHWnd != IntPtr.Zero)
            {
                this.BeginInvoke(new Action(() =>
                    NativeMethods.SendCopyData(_managerHWnd, this.Handle, MsgReady, _values.Count.ToString())));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            using Font statusFont = new Font("Segoe UI", 9f);
            using Font valueFont = new Font("Consolas", 9f);
            g.DrawString(_status, statusFont, Brushes.Black, 10, 8);

            // Значення у декількох стовпчиках та рядках
            const int cols = 4;
            float cellW = (this.ClientSize.Width - 20f) / cols;
            for (int i = 0; i < _values.Count; i++)
            {
                float x = 10 + (i % cols) * cellW;
                float y = 40 + (i / cols) * 18f;
                if (y > this.ClientSize.Height - 20) break;
                g.DrawString($"[{i}] {_values[i]:F2}", valueFont, Brushes.DarkBlue, x, y);
            }
        }
    }
}
```

### Object3/src/MainWindow.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object3
{
    // Object3 (варіант 3, Ж = 7): за командою READ_CLIPBOARD від Manager
    // зчитує дані з Clipboard Windows і відображає графік y=f(x), де
    // y — значення вектора, x — індекси елементів. Графік, як у математиці:
    // лінія через точки (x,y) у порядку зростання x; осі координат
    // з підписами числових значень x та y.
    public class Object3Form : Form
    {
        private const int MsgReadClipboard = 3; // Manager -> Object3

        private readonly List<double> _values = new List<double>();
        private string _status = "Очікування команди READ_CLIPBOARD від Manager...";

        public Object3Form()
        {
            this.Text = "Object 3 - Graph y=f(x)";
            this.Size = new Size(620, 520);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(870, 40); // праворуч — усі вікна видні
            this.DoubleBuffered = true;

            // Якщо у Clipboard уже є дані системи — показуємо їх одразу
            this.Load += (s, e) => TryReadClipboard();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_COPYDATA)
            {
                var (dwData, text) = NativeMethods.ParseCopyData(m);
                if (dwData.ToInt64() == MsgReadClipboard)
                {
                    TryReadClipboard();
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void TryReadClipboard()
        {
            try
            {
                string data = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(data)) return;

                // Розбір вектора значень; якщо дані чужі/пошкоджені —
                // зберігаємо попередній стан (захист від конфлікту буферів)
                var parsed = new List<double>();
                foreach (var s in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                        return;
                    parsed.Add(v);
                }
                if (parsed.Count == 0) return;

                _values.Clear();
                _values.AddRange(parsed);
                _status = $"Отримано {_values.Count} значень з Clipboard. Графік y=f(x), x — індекс елемента.";
            }
            catch (Exception ex)
            {
                _status = "Clipboard недоступний: " + ex.Message;
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using Font font = new Font("Segoe UI", 8.5f);
            g.DrawString(_status, font, Brushes.Black, 10, 6);

            if (_values.Count < 2)
            {
                g.DrawString("Недостатньо даних для побудови графіка.", font, Brushes.Gray, 30, 60);
                return;
            }

            // Область побудови з полями для осей і підписів
            int ml = 58, mr = 24, mt = 36, mb = 44;
            int pw = this.ClientSize.Width - ml - mr;
            int ph = this.ClientSize.Height - mt - mb;
            if (pw <= 10 || ph <= 10) return;

            double yMin = double.MaxValue, yMax = double.MinValue;
            foreach (var v in _values)
            {
                if (v < yMin) yMin = v;
                if (v > yMax) yMax = v;
            }
            double pad = (yMax - yMin) * 0.08;
            if (pad < 1e-9) pad = 1.0; // усі значення рівні
            yMin -= pad; yMax += pad;

            int n = _values.Count;
            Func<int, float> mapX = i => ml + pw * (i / (float)(n - 1));
            Func<double, float> mapY = v => mt + ph * (1f - (float)((v - yMin) / (yMax - yMin)));

            // Осі координат
            using Pen axisPen = new Pen(Color.Black, 1.4f);
            g.DrawLine(axisPen, ml, mt, ml, mt + ph);           // вісь Y
            g.DrawLine(axisPen, ml, mt + ph, ml + pw, mt + ph); // вісь X

            // Підписи числових значень x (індекси елементів)
            int xTicks = Math.Min(n - 1, 8);
            for (int t = 0; t <= xTicks; t++)
            {
                int idx = (int)Math.Round(t * (n - 1) / (double)xTicks);
                float x = mapX(idx);
                g.DrawLine(axisPen, x, mt + ph, x, mt + ph + 4);
                g.DrawString(idx.ToString(), font, Brushes.Black, x - 6, mt + ph + 6);
            }

            // Підписи числових значень y
            const int yTicks = 5;
            for (int t = 0; t <= yTicks; t++)
            {
                double v = yMin + (yMax - yMin) * t / (double)yTicks;
                float y = mapY(v);
                g.DrawLine(axisPen, ml - 4, y, ml, y);
                g.DrawString(v.ToString("F1"), font, Brushes.Black, 2, y - 7);
            }

            g.DrawString("x", font, Brushes.Black, ml + pw + 6, mt + ph - 8);
            g.DrawString("y", font, Brushes.Black, ml - 10, mt - 20);

            // Графік: лінія через точки (x, y) у порядку зростання x
            PointF[] pts = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                pts[i] = new PointF(mapX(i), mapY(_values[i]));
            }

            using Pen graphPen = new Pen(Color.Red, 2f);
            g.DrawLines(graphPen, pts);
            foreach (var pt in pts)
            {
                g.FillEllipse(Brushes.Red, pt.X - 2.5f, pt.Y - 2.5f, 5f, 5f);
            }
        }
    }
}
```

### Program.cs (усі три проєкти — однаковий патерн)
```csharp
using System;
using System.Windows.Forms;

namespace Lab6.Manager // Lab6.Object2 / Lab6.Object3
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ManagerForm());
        }
    }
}
```

---

## Запуск

```powershell
cd "E:\oop\oop\2026\labs\lab6\code"
dotnet build Lab6.slnx -c Debug
.\bin\Debug\Manager.exe
```

Усі три exe збираються у спільну папку `bin\Debug` (спільний `OutputPath` у csproj), тому Manager знаходить Object2.exe/Object3.exe поруч із собою. Далі: ввести n, Min, Max → «Виконати» → система автоматично запускає Object2/Object3, передає параметри WM_COPYDATA, Object3 будує графік. Закриття Manager закриває Object2/Object3.
