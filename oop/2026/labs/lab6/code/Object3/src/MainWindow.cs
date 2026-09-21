using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Lab6.Object3;

public class Object3Form : Form
{
    private const int MsgReadClipboard = 3; // Manager -> Object3
    private const int MaxClipboardLength = 1_000_000; // Defensive limit (1MB)

    private readonly List<double> _values = [];
    private string _status = "Очікування команди READ_CLIPBOARD від Manager...";

    public Object3Form()
    {
        Text = "Object 3 - Graph y=f(x) (.NET 8 Refactored)";
        Size = new Size(640, 530);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(880, 40);
        DoubleBuffered = true;

        Load += (_, _) => TryReadClipboard();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_COPYDATA)
        {
            var (dwData, _) = NativeMethods.ParseCopyData(m);
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
            if (string.IsNullOrWhiteSpace(data) || data.Length > MaxClipboardLength)
            {
                return;
            }

            // Zero-allocation Span-based parsing without string.Split or double array allocations
            ReadOnlySpan<char> span = data.AsSpan();
            List<double> parsed = [];

            while (!span.IsEmpty)
            {
                int sepIndex = span.IndexOf(';');
                ReadOnlySpan<char> token = sepIndex < 0 ? span : span[..sepIndex];
                span = sepIndex < 0 ? ReadOnlySpan<char>.Empty : span[(sepIndex + 1)..];

                token = token.Trim();
                if (token.IsEmpty) continue;

                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    return; // Ignore invalid clipboard content
                }
                parsed.Add(v);
            }

            if (parsed.Count == 0) return;

            _values.Clear();
            _values.AddRange(parsed);
            _status = $"Отримано {_values.Count} значень з Clipboard. Графік y=f(x), x — індекс елемента.";
        }
        catch (Exception ex)
        {
            _status = $"Clipboard недоступний: {ex.Message}";
        }
        Invalidate();
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

        int ml = 58, mr = 24, mt = 36, mb = 44;
        int pw = ClientSize.Width - ml - mr;
        int ph = ClientSize.Height - mt - mb;
        if (pw <= 10 || ph <= 10) return;

        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var v in _values)
        {
            if (v < yMin) yMin = v;
            if (v > yMax) yMax = v;
        }
        double pad = (yMax - yMin) * 0.08;
        if (pad < 1e-9) pad = 1.0;
        yMin -= pad; yMax += pad;

        int n = _values.Count;
        Func<int, float> mapX = i => ml + pw * (i / (float)(n - 1));
        Func<double, float> mapY = v => mt + ph * (1f - (float)((v - yMin) / (yMax - yMin)));

        using Pen axisPen = new Pen(Color.Black, 1.4f);
        g.DrawLine(axisPen, ml, mt, ml, mt + ph);
        g.DrawLine(axisPen, ml, mt + ph, ml + pw, mt + ph);

        int xTicks = Math.Min(n - 1, 8);
        for (int t = 0; t <= xTicks; t++)
        {
            int idx = (int)Math.Round(t * (n - 1) / (double)xTicks);
            float x = mapX(idx);
            g.DrawLine(axisPen, x, mt + ph, x, mt + ph + 4);
            g.DrawString(idx.ToString(CultureInfo.InvariantCulture), font, Brushes.Black, x - 6, mt + ph + 6);
        }

        const int yTicks = 5;
        for (int t = 0; t <= yTicks; t++)
        {
            double v = yMin + (yMax - yMin) * t / yTicks;
            float y = mapY(v);
            g.DrawLine(axisPen, ml - 4, y, ml, y);
            g.DrawString(v.ToString("F1", CultureInfo.InvariantCulture), font, Brushes.Black, 2, y - 7);
        }

        g.DrawString("x", font, Brushes.Black, ml + pw + 6, mt + ph - 8);
        g.DrawString("y", font, Brushes.Black, ml - 10, mt - 20);

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

