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
    // лінія, що проходить через точки (x,y) у порядку зростання x;
    // осі координат із підписами числових значень x та y.
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
