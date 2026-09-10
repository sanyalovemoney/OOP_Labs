using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Lab5.Shapes;

namespace Lab5
{
    public class MyEditor
    {
        // Singleton Меєрса (C#-аналог): Lazy<T> у режимі ExecutionAndPublication
        // гарантує єдиний екземпляр із лінивою потокобезпечною ініціалізацією —
        // так само, як static-локальна змінна у C++-функції getInstance():
        //   static Singleton& getInstance() { static Singleton instance; return instance; }
        // (Варіант завдання: непарний номер у списку, Ж = 7 → Singleton Меєрса)
        private static readonly Lazy<MyEditor> _lazy =
            new Lazy<MyEditor>(() => new MyEditor());

        public static MyEditor Instance => _lazy.Value;

        private readonly object _sync = new object();
        private readonly List<Shape> _shapes = new List<Shape>();

        private MyEditor() { }

        public void AddShape(Shape shape)
        {
            lock (_sync)
            {
                _shapes.Add(shape);
            }
        }

        // Read-only view: callers cannot mutate the internal list
        public IReadOnlyList<Shape> GetShapes() => _shapes.AsReadOnly();

        public void DrawAll(Graphics g, Pen pen)
        {
            List<Shape> snapshot;
            lock (_sync)
            {
                snapshot = new List<Shape>(_shapes);
            }

            foreach (var shape in snapshot)
            {
                shape.Draw(g, pen, GetFillBrush(shape));
            }
        }

        // Single place where fill colors are defined
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
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (var s in GetShapes())
                {
                    sw.WriteLine($"{s.GetName()}\t{s.X1}\t{s.Y1}\t{s.X2}\t{s.Y2}");
                }
            }
        }

        // Бонус методички (п.3): завантаження множини об'єктів з файлу.
        // Формат рядка: Name<TAB>x1<TAB>y1<TAB>x2<TAB>y2
        public int LoadFromFile(string path)
        {
            var loaded = new List<Shape>();
            foreach (var line in File.ReadAllLines(path))
            {
                string[] p = line.Split('\t');
                if (p.Length < 5) continue;
                if (!int.TryParse(p[1], out int x1) || !int.TryParse(p[2], out int y1) ||
                    !int.TryParse(p[3], out int x2) || !int.TryParse(p[4], out int y2)) continue;

                Shape? shape = CreateShapeByName(p[0], x1, y1, x2, y2);
                if (shape != null) loaded.Add(shape);
            }

            lock (_sync)
            {
                _shapes.Clear();
                _shapes.AddRange(loaded);
            }
            return loaded.Count;
        }

        // Фабрика відновлення об'єктів за іменем класу (поліморфізм)
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
}
