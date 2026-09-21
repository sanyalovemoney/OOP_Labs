using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab6.Manager;

public class ManagerForm : Form
{
    internal const int MsgParams = 1;        // Manager -> Object2
    internal const int MsgReady = 2;         // Object2 -> Manager
    internal const int MsgReadClipboard = 3; // Manager -> Object3

    private readonly TextBox _txtN;
    private readonly TextBox _txtMin;
    private readonly TextBox _txtMax;
    private readonly Button _btnRun;
    private readonly Label _lblStatus;
    private int _yPos = 20;

    private readonly ConcurrentBag<Process> _companions = [];
    private CancellationTokenSource? _cts;
    private IntPtr _object3HWnd = IntPtr.Zero;

    public ManagerForm()
    {
        Text = "Lab 6 - Manager (.NET 8 Refactored)";
        Size = new Size(360, 320);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(30, 40);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        AddInput("n (2..1000):", _txtN = new TextBox { Text = "10" });
        AddInput("Min:", _txtMin = new TextBox { Text = "0" });
        AddInput("Max:", _txtMax = new TextBox { Text = "100" });

        _btnRun = new Button { Text = "Виконати", Location = new Point(110, _yPos + 10), Size = new Size(120, 32) };
        _btnRun.Click += OnRunClicked;
        Controls.Add(_btnRun);

        _lblStatus = new Label
        {
            Text = "Готовий до запуску системи.",
            Location = new Point(20, _yPos + 55),
            Size = new Size(310, 55)
        };
        Controls.Add(_lblStatus);

        FormClosed += (_, _) => CloseCompanions();
    }

    private void AddInput(string labelText, TextBox tb)
    {
        Label lbl = new Label { Text = labelText, Location = new Point(20, _yPos), AutoSize = true };
        tb.Location = new Point(130, _yPos - 3);
        tb.Size = new Size(110, 22);
        Controls.Add(lbl);
        Controls.Add(tb);
        _yPos += 36;
    }

    private async void OnRunClicked(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        if (!int.TryParse(_txtN.Text, out int n) || n is < 2 or > 1000)
        {
            MessageBox.Show("Кількість значень n: ціле число від 2 до 1000.",
                "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!double.TryParse(_txtMin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double min) ||
            !double.TryParse(_txtMax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double max) || min >= max)
        {
            MessageBox.Show("Діапазон: дійсні числа Min < Max.",
                "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnRun.Enabled = false;
        _lblStatus.Text = "Пошук/запуск Object2 та Object3...";

        try
        {
            Process p2 = FindOrStart("Object2");
            Process p3 = FindOrStart("Object3");

            IntPtr h2 = await WaitForWindowAsync(p2, token: token);
            IntPtr h3 = await WaitForWindowAsync(p3, token: token);

            if (h2 == IntPtr.Zero || h3 == IntPtr.Zero)
            {
                _lblStatus.Text = "Помилка: не вдалося знайти вікна Object2/Object3 (таймаут).";
                return;
            }

            _object3HWnd = h3;

            string payload = string.Join(";",
                n.ToString(CultureInfo.InvariantCulture),
                min.ToString(CultureInfo.InvariantCulture),
                max.ToString(CultureInfo.InvariantCulture));

            bool sent = NativeMethods.SendCopyData(h2, Handle, MsgParams, payload);
            _lblStatus.Text = sent
                ? $"Параметри \"{payload}\" надіслані Object2 (WM_COPYDATA). Очікування READY..."
                : "Помилка надсилання даних у Object2 (таймаут/блокування).";
        }
        catch (OperationCanceledException)
        {
            _lblStatus.Text = "Операцію скасовано.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            _btnRun.Enabled = true;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_COPYDATA)
        {
            var (dwData, text) = NativeMethods.ParseCopyData(m);
            if (dwData.ToInt64() == MsgReady && _object3HWnd != IntPtr.Zero)
            {
                bool sent = NativeMethods.SendCopyData(_object3HWnd, Handle, MsgReadClipboard, text);
                _lblStatus.Text = sent
                    ? $"READY від Object2 (значень: {text}). Object3 будує графік y=f(x)."
                    : "Помилка надсилання команди у Object3.";
            }
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    private Process FindOrStart(string name)
    {
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
            catch
            {
                // Процес недоступний / завершується
            }
        }

        string exePath = Path.Combine(AppContext.BaseDirectory, $"{name}.exe");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Виконуваний файл не знайдено: {exePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        Process started = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не вдалося запустити процес {name}");

        TrackCompanion(started);
        return started;
    }

    private void TrackCompanion(Process p)
    {
        if (!_companions.Contains(p))
        {
            _companions.Add(p);
        }
    }

    private static async Task<IntPtr> WaitForWindowAsync(Process p, int timeoutMs = 10000, CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            token.ThrowIfCancellationRequested();
            p.Refresh();
            if (p.HasExited) return IntPtr.Zero;
            if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
            await Task.Delay(100, token);
        }
        return IntPtr.Zero;
    }

    private void CloseCompanions()
    {
        _cts?.Cancel();
        foreach (var p in _companions)
        {
            try
            {
                p.Refresh();
                if (!p.HasExited)
                {
                    p.CloseMainWindow();
                }
            }
            catch
            {
                // Ігноруємо помилки закриття завершених процесів
            }
        }
    }
}

