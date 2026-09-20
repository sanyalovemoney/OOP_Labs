using System;
using System.Drawing;
using System.Windows.Forms;
using Lab4.Shapes;

namespace Lab4;

public class MainWindow : Form
{
    private enum ShapeType { Point, Line, Rectangle, Ellipse, LineCircles, Cube }
    private ShapeType _currentType = ShapeType.Line;
    private readonly MyEditor _editor = new();

    private Point _startPoint;
    private Point _currentPoint;
    private bool _isDrawing;

    private MenuStrip? _menuStrip;
    private ToolStrip? _toolStrip;

    public MainWindow()
    {
        Text = "Graphic Object Editor - Lab 4 (.NET 8 Refactored)";
        Size = new Size(800, 600);
        DoubleBuffered = true;

        InitializeMenu();
        InitializeToolbar();
    }

    private void InitializeMenu()
    {
        _menuStrip = new MenuStrip();
        var objectsMenu = new ToolStripMenuItem("Objects");

        objectsMenu.DropDownItems.Add("Point", null, (_, _) => { _currentType = ShapeType.Point; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Line", null, (_, _) => { _currentType = ShapeType.Line; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Rectangle", null, (_, _) => { _currentType = ShapeType.Rectangle; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Ellipse", null, (_, _) => { _currentType = ShapeType.Ellipse; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("LineWithCircles", null, (_, _) => { _currentType = ShapeType.LineCircles; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Cube", null, (_, _) => { _currentType = ShapeType.Cube; UpdateTitle(); });

        _menuStrip.Items.Add(objectsMenu);
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);

        UpdateTitle();
    }

    private void InitializeToolbar()
    {
        _toolStrip = new ToolStrip();

        var btnPoint = new ToolStripButton("Point") { ToolTipText = "Point" };
        btnPoint.Click += (_, _) => { _currentType = ShapeType.Point; UpdateTitle(); };
        _toolStrip.Items.Add(btnPoint);

        var btnLine = new ToolStripButton("Line") { ToolTipText = "Line" };
        btnLine.Click += (_, _) => { _currentType = ShapeType.Line; UpdateTitle(); };
        _toolStrip.Items.Add(btnLine);

        var btnRect = new ToolStripButton("Rect") { ToolTipText = "Rectangle" };
        btnRect.Click += (_, _) => { _currentType = ShapeType.Rectangle; UpdateTitle(); };
        _toolStrip.Items.Add(btnRect);

        var btnEllipse = new ToolStripButton("Ellipse") { ToolTipText = "Ellipse" };
        btnEllipse.Click += (_, _) => { _currentType = ShapeType.Ellipse; UpdateTitle(); };
        _toolStrip.Items.Add(btnEllipse);

        var btnLCirc = new ToolStripButton("L-Circ") { ToolTipText = "Line with Circles" };
        btnLCirc.Click += (_, _) => { _currentType = ShapeType.LineCircles; UpdateTitle(); };
        _toolStrip.Items.Add(btnLCirc);

        var btnCube = new ToolStripButton("Cube") { ToolTipText = "Cube Wireframe" };
        btnCube.Click += (_, _) => { _currentType = ShapeType.Cube; UpdateTitle(); };
        _toolStrip.Items.Add(btnCube);

        Controls.Add(_toolStrip);
    }

    private void UpdateTitle() => Text = $"Graphic Object Editor - Lab 4 [{_currentType}]";

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
        if (_isDrawing && _currentPoint != e.Location)
        {
            _currentPoint = e.Location;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isDrawing && e.Button == MouseButtons.Left)
        {
            _isDrawing = false;
            _editor.AddShape(CreateShape(_currentType, _startPoint, e.Location));
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using Pen pen = new Pen(Color.Black, 1);
        _editor.DrawAll(g, pen);

        if (_isDrawing)
        {
            using Pen rubberPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            using Brush rubberBrush = new SolidBrush(Color.FromArgb(100, Color.LightGray));
            Shape rubberShape = CreateShape(_currentType, _startPoint, _currentPoint);
            rubberShape.Draw(g, rubberPen, rubberBrush);
        }
    }
}

