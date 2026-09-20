using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using Lab5.Shapes;

namespace Lab5;

public class MyEditor
{
    private static readonly Lazy<MyEditor> _lazy = new(() => new MyEditor());
    public static MyEditor Instance => _lazy.Value;

    private readonly object _sync = new();
    private readonly List<Shape> _shapes = [];

    private MyEditor() { }

    public void AddShape(Shape shape)
    {
        lock (_sync)
        {
            _shapes.Add(shape);
        }
    }

    /// <summary>
    /// Thread-safe snapshot view of shapes. Prevents race conditions during iteration.
    /// </summary>
    public IReadOnlyList<Shape> GetShapes()
    {
        lock (_sync)
        {
            return _shapes.ToArray();
        }
    }

    public void DrawAll(Graphics g, Pen pen)
    {
        IReadOnlyList<Shape> snapshot = GetShapes();
        foreach (var shape in snapshot)
        {
            shape.Draw(g, pen, GetFillBrush(shape));
        }
    }

    private static Brush GetFillBrush(Shape shape) => shape switch
    {
        RectShape => Brushes.Orange,
        EllipseShape => Brushes.White,
        LineWithCirclesShape => Brushes.Yellow,
        CubeWireframeShape => Brushes.Cyan,
        _ => Brushes.LightGray
    };

    public void SaveToFile(string path)
    {
        using var sw = new StreamWriter(path);
        foreach (var s in GetShapes())
        {
            sw.WriteLine($"{s.GetName()}\t{s.X1}\t{s.Y1}\t{s.X2}\t{s.Y2}");
        }
    }

    public int LoadFromFile(string path)
    {
        var loaded = new List<Shape>();
        Span<Range> ranges = stackalloc Range[6];

        foreach (var line in File.ReadAllLines(path))
        {
            ReadOnlySpan<char> span = line.AsSpan().Trim();
            if (span.IsEmpty) continue;

            int count = span.Split(ranges, '\t', StringSplitOptions.RemoveEmptyEntries);
            if (count < 5) continue;

            string name = span[ranges[0]].ToString();
            if (!int.TryParse(span[ranges[1]], CultureInfo.InvariantCulture, out int x1) ||
                !int.TryParse(span[ranges[2]], CultureInfo.InvariantCulture, out int y1) ||
                !int.TryParse(span[ranges[3]], CultureInfo.InvariantCulture, out int x2) ||
                !int.TryParse(span[ranges[4]], CultureInfo.InvariantCulture, out int y2))
            {
                continue;
            }

            Shape? shape = CreateShapeByName(name, x1, y1, x2, y2);
            if (shape != null) loaded.Add(shape);
        }

        lock (_sync)
        {
            _shapes.Clear();
            _shapes.AddRange(loaded);
        }
        return loaded.Count;
    }


    private static Shape? CreateShapeByName(string name, int x1, int y1, int x2, int y2) => name switch
    {
        "Point" => new PointShape(x1, y1, x2, y2),
        "Line" => new LineShape(x1, y1, x2, y2),
        "Rectangle" => new RectShape(x1, y1, x2, y2),
        "Ellipse" => new EllipseShape(x1, y1, x2, y2),
        "LineWithCircles" => new LineWithCirclesShape(x1, y1, x2, y2),
        "CubeWireframe" => new CubeWireframeShape(x1, y1, x2, y2),
        _ => null
    };
}

