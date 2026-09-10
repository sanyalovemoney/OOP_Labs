using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object2
{
    // Object2 (варіант 3, Ж = 7): отримує параметри n;Min;Max повідомленням
    // WM_COPYDATA від Manager, створює вектор із n дробових (double) чисел
    // у діапазоні Min–Max, показує значення у декількох стовпчиках та рядках
    // у власному головному вікні, записує дані у Clipboard у текстовому
    // форматі та надсилає Manager повідомлення-відповідь READY.
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

            // Запис даних у Clipboard у текстовому форматі (згідно варіанту).
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

            // Повідомлення-відповідь Manager надсилаємо асинхронно (BeginInvoke),
            // щоб уникнути перехресного блокування SendMessage під час обробки
            // вхідного WM_COPYDATA
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
