# Лабораторна робота №2: Графічний редактор об'єктів

## Опис
Ця лабораторна робота присвячена впровадженню принципів об'єктно-орієнтованого програмування: абстракції, успадкування та поліморфізму. Створено графічний редактор, що дозволяє малювати різні типи геометричних фігур.

## Реалізація
Програма написана на C# (WinForms).

### 1. Ієрархія класів `Shape`
Створено базовий абстрактний клас `Shape`, який визначає загальні властивості (координати) та абстрактний метод `Draw`.

- **PointShape**: Малює точку (малий квадрат).
- **LineShape**: Малює відрізок між двома точками.
- **RectShape**: Малює заповнений прямокутник з рамкою.
- **EllipseShape**: Малює заповнений еліпс з рамкою.

### 2. Функціонал редактора (`MainWindow.cs`)
- **Вибір об'єкта**: Через меню «Objects» користувач обирає тип фігури.
- **Малювання**: Використовуються події миші. Координати фігури визначаються від моменту натискання (X1, Y1) до моменту відпускання кнопки миші (X2, Y2).
- **Ефект «гумової стрічки»**: Під час руху миші (MouseMove) програма постійно перемальовує тимчасову фігуру, що дозволяє бачити майбутній результат.
- **Поліморфізм**: Всі створені об'єкти зберігаються у списку `List<Shape>`. При перемальовуванні вікна викликається метод `Draw` для кожного об'єкта, незалежно від його конкретного типу.

---

## Вихідний код

### Shape.cs (Базовий клас)
```csharp
using System.Drawing;

namespace Lab2.Shapes
{
    public abstract class Shape
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public Shape(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public abstract void Draw(Graphics g, Pen pen, Brush brush);
    }
}
```

### PointShape.cs
```csharp
using System.Drawing;

namespace Lab2.Shapes
{
    public class PointShape : Shape
    {
        public PointShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            g.FillRectangle(brush, X1, Y1, 2, 2);
        }
    }
}
```

### LineShape.cs
```csharp
using System.Drawing;

namespace Lab2.Shapes
{
    public class LineShape : Shape
    {
        public LineShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            g.DrawLine(pen, X1, Y1, X2, Y2);
        }
    }
}
```

### RectShape.cs
```csharp
using System.Drawing;

namespace Lab2.Shapes
{
    public class RectShape : Shape
    {
        public RectShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2);
            int y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2);
            int h = Math.Abs(Y1 - Y2);

            g.FillRectangle(brush, x, y, w, h);
            g.DrawRectangle(pen, x, y, w, h);
        }
    }
}
```

### EllipseShape.cs
```csharp
using System.Drawing;

namespace Lab2.Shapes
{
    public class EllipseShape : Shape
    {
        public EllipseShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2);
            int y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2);
            int h = Math.Abs(Y1 - Y2);

            g.FillEllipse(brush, x, y, w, h);
            g.DrawEllipse(pen, x, y, w, h);
        }
    }
}
```

### MainWindow.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lab2.Shapes;

namespace Lab2
{
    public partial class MainWindow : Form
    {
        private enum ShapeType { Point, Line, Rectangle, Ellipse }
        private ShapeType _currentType = ShapeType.Line;
        private List<Shape> _shapes = new List<Shape>();

        private Point _startPoint;
        private Point _currentPoint;
        private bool _isDrawing = false;

        private MenuStrip _menuStrip;

        public MainWindow()
        {
            this.Text = "Graphic Object Editor - Lab 2";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            InitializeMenu();
        }

        private void InitializeMenu()
        {
            _menuStrip = new MenuStrip();
            var objectsMenu = new ToolStripMenuItem("Objects");

            objectsMenu.DropDownItems.Add("Point", null, (s, e) => { _currentType = ShapeType.Point; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Line", null, (s, e) => { _currentType = ShapeType.Line; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Rectangle", null, (s, e) => { _currentType = ShapeType.Rectangle; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Ellipse", null, (s, e) => { _currentType = ShapeType.Ellipse; UpdateTitle(); });

            _menuStrip.Items.Add(objectsMenu);
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            this.Text = $"Graphic Object Editor - Lab 2 [{_currentType}]";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDrawing = true;
                _startPoint = e.Location;
                _currentPoint = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDrawing)
            {
                _currentPoint = e.Location;
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDrawing && e.Button == MouseButtons.Left)
            {
                _isDrawing = false;
                Shape shape = null;

                switch (_currentType)
                {
                    case ShapeType.Point:
                        shape = new PointShape(_startPoint.X, _startPoint.Y, e.Location.X, e.Location.Y);
                        break;
                    case ShapeType.Line:
                        shape = new LineShape(_startPoint.X, _startPoint.Y, e.Location.X, e.Location.Y);
                        break;
                    case ShapeType.Rectangle:
                        shape = new RectShape(_startPoint.X, _startPoint.Y, e.Location.X, e.Location.Y);
                        break;
                    case ShapeType.Ellipse:
                        shape = new EllipseShape(_startPoint.X, _startPoint.Y, e.Location.X, e.Location.Y);
                        break;
                }

                if (shape != null)
                {
                    _shapes.Add(shape);
                }
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Pen pen = new Pen(Color.Black, 1);
            Brush brush = Brushes.White;

            foreach (var shape in _shapes)
            {
                if (shape is RectShape) brush = Brushes.Orange;
                else if (shape is EllipseShape) brush = Brushes.White;
                else brush = Brushes.LightGray;

                shape.Draw(g, pen, brush);
            }

            if (_isDrawing)
            {
                Pen rubberPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                Brush rubberBrush = new SolidBrush(Color.FromArgb(100, Color.LightGray));

                Shape rubberShape = null;
                switch (_currentType)
                {
                    case ShapeType.Point:
                        rubberShape = new PointShape(_startPoint.X, _startPoint.Y, _currentPoint.X, _currentPoint.Y);
                        break;
                    case ShapeType.Line:
                        rubberShape = new LineShape(_startPoint.X, _startPoint.Y, _currentPoint.X, _currentPoint.Y);
                        break;
                    case ShapeType.Rectangle:
                        rubberShape = new RectShape(_startPoint.X, _startPoint.Y, _currentPoint.X, _currentPoint.Y);
                        break;
                    case ShapeType.Ellipse:
                        rubberShape = new EllipseShape(_startPoint.X, _startPoint.Y, _currentPoint.X, _currentPoint.Y);
                        break;
                }

                if (rubberShape != null)
                {
                    rubberShape.Draw(g, rubberPen, rubberBrush);
                }
            }
        }
    }
}
```
