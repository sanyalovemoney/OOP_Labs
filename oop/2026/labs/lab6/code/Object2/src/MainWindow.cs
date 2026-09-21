using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace Lab6.Object2;

public class Object2Form : Form
{
    private const int MsgParams = 1; // Manager -> Object2
    private const int MsgReady = 2;  // Object2 -> Manager

    private readonly List<double> _values = [];
    private IntPtr _managerHWnd = IntPtr.Zero;
    private string _status = "Очікування параметрів (n;Min;Max) від Manager через WM_COPYDATA...";

    public Object2Form()
    {
        Text = "Object 2 - Data Gen (.NET 8 Refactored)";
        Size = new Size(460, 380);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(410, 40);
        DoubleBuffered = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_COPYDATA)
        {
            var (dwData, text) = NativeMethods.ParseCopyData(m);
            if (dwData.ToInt64() == MsgParams)
            {
                _managerHWnd = m.WParam;
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
            !int.TryParse(p[0], out int n) || n is < 2 or > 1000 ||
            !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double min) ||
            !double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double max) ||
            min >= max)
        {
            _status = $"Отримано некоректні параметри: \"{text}\"";
            Invalidate();
            return;
        }

        // Random.Shared (.NET 6+) prevents unnecessary allocations & thread seeding issues
        _values.Clear();
        _values.EnsureCapacity(n);
        for (int i = 0; i < n; i++)
        {
            _values.Add(min + Random.Shared.NextDouble() * (max - min));
        }

        // High-performance string construction using pre-allocated StringBuilder
        var sb = new StringBuilder(n * 10);
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(_values[i].ToString("F4", CultureInfo.InvariantCulture));
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            _status = $"Вектор із {n} double-значень у діапазоні [{min}; {max}] створено. Дані записано у Clipboard.";
        }
        catch (Exception ex)
        {
            _status = $"Помилка запису у Clipboard: {ex.Message}";
        }
        Invalidate();

        if (_managerHWnd != IntPtr.Zero)
        {
            BeginInvoke(new Action(() =>
                NativeMethods.SendCopyData(_managerHWnd, Handle, MsgReady, _values.Count.ToString(CultureInfo.InvariantCulture))));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        using Font statusFont = new Font("Segoe UI", 9f);
        using Font valueFont = new Font("Consolas", 9f);
        g.DrawString(_status, statusFont, Brushes.Black, 10, 8);

        const int cols = 4;
        float cellW = (ClientSize.Width - 20f) / cols;
        for (int i = 0; i < _values.Count; i++)
        {
            float x = 10 + (i % cols) * cellW;
            float y = 40 + (i / cols) * 18f;
            if (y > ClientSize.Height - 20) break;
            g.DrawString($"[{i}] {_values[i]:F2}", valueFont, Brushes.DarkBlue, x, y);
        }
    }
}

