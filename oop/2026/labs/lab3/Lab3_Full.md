# Лабораторна робота №3: Інтерфейс користувача та Toolbar

## Опис
Метою цієї лабораторної роботи було вдосконалення інтерфейсу графічного редактора шляхом додавання панелі інструментів (Toolbar) та підказок (Tooltips) для покращення взаємодії з користувачем.

## Реалізація
Програма написана на C# (WinForms).

### 1. Панель інструментів (Toolbar)
У головне вікно додано компонент `ToolStrip`, який містить кнопки швидкого доступу до вибору фігур:
- **Point**: Кнопка для вибору інструменту малювання точок.
- **Line**: Кнопка для вибору інструменту малювання ліній.
- **Rect**: Кнопка для вибору інструменту малювання прямокутників.
- **Ellipse**: Кнопка для вибору інструменту малювання еліпсів.

### 2. Підказки (Tooltips)
Кожна кнопка на Toolbar має властивість `ToolTipText`. При наведенні курсору миші на кнопку користувач бачить підказку, що пояснює функцію цієї кнопки.

### 3. Покращення інтерфейсу
- Вибір фігури через Toolbar синхронізовано з вибором через меню.
- Заголовок вікна оновлюється динамічно, відображаючи поточний активний інструмент.

---

## Вихідний код

### MainWindow.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lab3.Shapes;

namespace Lab3
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
        private ToolStrip _toolStrip;

        public MainWindow()
        {
            this.Text = "Graphic Object Editor - Lab 3";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            InitializeMenu();
            InitializeToolbar();
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

        private void InitializeToolbar()
        {
            _toolStrip = new ToolStrip();
            
            var btnPoint = new ToolStripButton("Point") { ToolTipText = "Draw a Point" };
            btnPoint.Click += (s, e) => { _currentType = ShapeType.Point; UpdateTitle(); };
            
            var btnLine = new ToolStripButton("Line") { ToolTipText = "Draw a Line" };
            btnLine.Click += (s, e) => { _currentType = ShapeType.Line; UpdateTitle(); };
            
            var btnRect = new ToolStripButton("Rect") { ToolTipText = "Draw a Rectangle" };
            btnRect.Click += (s, e) => { _currentType = ShapeType.Rectangle; UpdateTitle(); };
            
            var btnEllipse = new ToolStripButton("Ellipse") { ToolTipText = "Draw an Ellipse" };
            btnEllipse.Click += (s, e) => { _currentType = ShapeType.Ellipse; UpdateTitle(); };

            _toolStrip.Items.Add(btnPoint);
            _toolStrip.Items.Add(btnLine);
            _toolStrip.Items.Add(btnRect);
            _toolStrip.Items.Add(btnEllipse);

            this.Controls.Add(_toolStrip);
        }

        private void UpdateTitle()
        {
            this.Text = $"Graphic Object Editor - Lab 3 [{_currentType}]";
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

### Shape.cs (Базовий клас)
```csharp
using System.Drawing;

namespace Lab3.Shapes
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
