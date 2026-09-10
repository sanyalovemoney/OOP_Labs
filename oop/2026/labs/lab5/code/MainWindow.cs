using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lab5.Shapes;

namespace Lab5
{
    public class MainWindow : Form
    {
        private enum ShapeType { Point, Line, Rectangle, Ellipse, LineCircles, Cube }
        private ShapeType _currentType = ShapeType.Line;
        private Point _startPoint;
        private Point _currentPoint;
        private bool _isDrawing = false;

        private MenuStrip? _menuStrip;
        private ToolStrip? _toolStrip;
        private MyTableForm? _tableForm;

        public MainWindow()
        {
            this.Text = "Graphic Object Editor - Lab 5";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            InitializeMenu();
            InitializeToolbar();
        }

        private void InitializeMenu()
        {
            _menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("File");
            var loadItem = new ToolStripMenuItem("Load", null, OnLoadClicked);
            fileMenu.DropDownItems.Add(loadItem);
            var saveItem = new ToolStripMenuItem("Save", null, OnSaveClicked);
            fileMenu.DropDownItems.Add(saveItem);

            var viewMenu = new ToolStripMenuItem("View");
            var tableItem = new ToolStripMenuItem("Table", null, OnTableClicked);
            viewMenu.DropDownItems.Add(tableItem);

            var objectsMenu = new ToolStripMenuItem("Objects");
            objectsMenu.DropDownItems.Add("Point", null, (s, e) => { _currentType = ShapeType.Point; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Line", null, (s, e) => { _currentType = ShapeType.Line; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Rectangle", null, (s, e) => { _currentType = ShapeType.Rectangle; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Ellipse", null, (s, e) => { _currentType = ShapeType.Ellipse; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("LineWithCircles", null, (s, e) => { _currentType = ShapeType.LineCircles; UpdateTitle(); });
            objectsMenu.DropDownItems.Add("Cube", null, (s, e) => { _currentType = ShapeType.Cube; UpdateTitle(); });

            _menuStrip.Items.Add(fileMenu);
            _menuStrip.Items.Add(viewMenu);
            _menuStrip.Items.Add(objectsMenu);
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);

            UpdateTitle();
        }

        private void InitializeToolbar()
        {
            _toolStrip = new ToolStrip();

            var btnPoint = new ToolStripButton("Point") { ToolTipText = "Point" };
            btnPoint.Click += (s, e) => { _currentType = ShapeType.Point; UpdateTitle(); };
            _toolStrip.Items.Add(btnPoint);

            var btnLine = new ToolStripButton("Line") { ToolTipText = "Line" };
            btnLine.Click += (s, e) => { _currentType = ShapeType.Line; UpdateTitle(); };
            _toolStrip.Items.Add(btnLine);

            var btnRect = new ToolStripButton("Rect") { ToolTipText = "Rectangle" };
            btnRect.Click += (s, e) => { _currentType = ShapeType.Rectangle; UpdateTitle(); };
            _toolStrip.Items.Add(btnRect);

            var btnEllipse = new ToolStripButton("Ellipse") { ToolTipText = "Ellipse" };
            btnEllipse.Click += (s, e) => { _currentType = ShapeType.Ellipse; UpdateTitle(); };
            _toolStrip.Items.Add(btnEllipse);

            var btnLCirc = new ToolStripButton("L-Circ") { ToolTipText = "Line with Circles" };
            btnLCirc.Click += (s, e) => { _currentType = ShapeType.LineCircles; UpdateTitle(); };
            _toolStrip.Items.Add(btnLCirc);

            var btnCube = new ToolStripButton("Cube") { ToolTipText = "Cube Wireframe" };
            btnCube.Click += (s, e) => { _currentType = ShapeType.Cube; UpdateTitle(); };
            _toolStrip.Items.Add(btnCube);
            this.Controls.Add(_toolStrip);
        }

        private void OnTableClicked(object? sender, EventArgs e)
        {
            if (_tableForm == null || _tableForm.IsDisposed)
            {
                _tableForm = new MyTableForm();
            }
            _tableForm.UpdateData(GetTableRows());
            _tableForm.Show();
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    MyEditor.Instance.SaveToFile(sfd.FileName);
                    MessageBox.Show("Saved successfully!");
                }
            }
        }

        // Бонус методички (п.3): завантаження множини об'єктів з файлу —
        // об'єкти відображаються і у головному вікні, і у вікні таблиці
        private void OnLoadClicked(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    int count = MyEditor.Instance.LoadFromFile(ofd.FileName);
                    this.Invalidate();
                    RefreshTableIfOpen();
                    MessageBox.Show($"Loaded {count} shapes from file.");
                }
            }
        }

        // Таблиця — незалежний модуль: передаємо їй прості дані, а не Shape-об'єкти
        private static IEnumerable<(string, int, int, int, int)> GetTableRows()
        {
            foreach (var s in MyEditor.Instance.GetShapes())
            {
                yield return (s.GetName(), s.X1, s.Y1, s.X2, s.Y2);
            }
        }

        // Вимога методички: при кожному додаванні нового об'єкта рядок
        // автоматично з'являється у вікні таблиці (якщо воно відкрите)
        private void RefreshTableIfOpen()
        {
            if (_tableForm != null && !_tableForm.IsDisposed && _tableForm.Visible)
            {
                _tableForm.UpdateData(GetTableRows());
            }
        }

        private void UpdateTitle() => this.Text = $"Graphic Object Editor - Lab 5 [{_currentType}]";

        // Factory: single place where shapes are created (OCP-friendly)
        private static Shape CreateShape(ShapeType type, Point start, Point end) => type switch
        {
            ShapeType.Point => new PointShape(start.X, start.Y, end.X, end.Y),
            ShapeType.Line => new LineShape(start.X, start.Y, end.X, end.Y),
            ShapeType.Rectangle => new RectShape(start.X, start.Y, end.X, end.Y),
            ShapeType.Ellipse => new EllipseShape(start.X, start.Y, end.X, end.Y),
            ShapeType.LineCircles => new LineWithCirclesShape(start.X, start.Y, end.X, end.Y),
            ShapeType.Cube => new CubeWireframeShape(start.X, start.Y, end.X, end.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

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
                MyEditor.Instance.AddShape(CreateShape(_currentType, _startPoint, e.Location));
                RefreshTableIfOpen(); // новий рядок автоматично з'являється у таблиці
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(Color.Black, 1))
            {
                MyEditor.Instance.DrawAll(g, pen);

                if (_isDrawing)
                {
                    using (Pen rubberPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    using (Brush rubberBrush = new SolidBrush(Color.FromArgb(100, Color.LightGray)))
                    {
                        Shape rubberShape = CreateShape(_currentType, _startPoint, _currentPoint);
                        rubberShape.Draw(g, rubberPen, rubberBrush);
                    }
                }
            }
        }
    }
}
