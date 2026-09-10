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
    //
    // Алгоритм взаємодії (рис. 6.3/6.4 методички):
    //   Manager --WM_COPYDATA(1: "n;Min;Max")--> Object2
    //   Object2 генерує вектор, показує його, записує у Clipboard,
    //   Object2 --WM_COPYDATA(2: "READY")------> Manager
    //   Manager --WM_COPYDATA(3: "READ")-------> Object3
    //   Object3 читає Clipboard і будує графік y=f(x)
    public class ManagerForm : Form
    {
        // Коди повідомлень (поле dwData структури COPYDATASTRUCT) —
        // однакові в усіх трьох програмах системи
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

            lblStatus = new Label
            {
                Text = "Готовий до запуску системи.",
                Location = new Point(20, _yPos + 55),
                Size = new Size(295, 50)
            };
            this.Controls.Add(lblStatus);

            // Вимога 5: після завершення Manager автоматично завершуються
            // і програми Object2 та Object3
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
                // Вимога 4: якщо програми вже запущені — використовуємо їх,
                // інакше запускаємо самі (без участі користувача)
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
