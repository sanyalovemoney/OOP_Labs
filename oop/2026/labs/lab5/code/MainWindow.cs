using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lab5.Shapes;

namespace Lab5;

public class MainWindow : Form
{
    private enum ShapeType { Point, Line, Rectangle, Ellipse, LineCircles, Cube }
    private ShapeType _currentType = ShapeType.Line;
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isDrawing;

    private MenuStrip? _menuStrip;
    private ToolStrip? _toolStrip;
    private MyTableForm? _tableForm;

    public MainWindow()
    {
        Text = "Graphic Object Editor - Lab 5 (.NET 8 Refactored)";
        Size = new Size(800, 600);
        DoubleBuffered = true;

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
        objectsMenu.DropDownItems.Add("Point", null, (_, _) => { _currentType = ShapeType.Point; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Line", null, (_, _) => { _currentType = ShapeType.Line; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Rectangle", null, (_, _) => { _currentType = ShapeType.Rectangle; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Ellipse", null, (_, _) => { _currentType = ShapeType.Ellipse; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("LineWithCircles", null, (_, _) => { _currentType = ShapeType.LineCircles; UpdateTitle(); });
        objectsMenu.DropDownItems.Add("Cube", null, (_, _) => { _currentType = ShapeType.Cube; UpdateTitle(); });

        _menuStrip.Items.Add(fileMenu);
        _menuStrip.Items.Add(viewMenu);
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

    private void OnTableClicked(object? sender, EventArgs e)
    {
        if (_tableForm == null || _tableForm.IsDisposed)
        {
            _tableForm = new MyTableForm();
        }
        _tableForm.UpdateData(GetTableRows());
        _tableForm.Show();
    }

    private static void OnSaveClicked(object? sender, EventArgs e)
    {
        using SaveFileDialog sfd = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            MyEditor.Instance.SaveToFile(sfd.FileName);
            MessageBox.Show("Saved successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnLoadClicked(object? sender, EventArgs e)
    {
        using OpenFileDialog ofd = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            int count = MyEditor.Instance.LoadFromFile(ofd.FileName);
            Invalidate();
            RefreshTableIfOpen();
            MessageBox.Show($"Loaded {count} shapes from file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static IEnumerable<(string, int, int, int, int)> GetTableRows()
    {
        foreach (var s in MyEditor.Instance.GetShapes())
        {
            yield return (s.GetName(), s.X1, s.Y1, s.X2, s.Y2);
        }
    }

    private void RefreshTableIfOpen()
    {
        if (_tableForm is { IsDisposed: false, Visible: true })
        {
            _tableForm.UpdateData(GetTableRows());
        }
    }

    private void UpdateTitle() => Text = $"Graphic Object Editor - Lab 5 [{_currentType}]";

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
            MyEditor.Instance.AddShape(CreateShape(_currentType, _startPoint, e.Location));
            RefreshTableIfOpen();
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using Pen pen = new Pen(Color.Black, 1);
        MyEditor.Instance.DrawAll(g, pen);

        if (_isDrawing)
        {
            using Pen rubberPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            using Brush rubberBrush = new SolidBrush(Color.FromArgb(100, Color.LightGray));
            Shape rubberShape = CreateShape(_currentType, _startPoint, _currentPoint);
            rubberShape.Draw(g, rubberPen, rubberBrush);
        }
    }
}

