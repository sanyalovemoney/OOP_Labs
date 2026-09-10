using System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace Lab5.Shapes
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
        public abstract string GetName();
    }

    public class PointShape : Shape
    {
        public PointShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush) => g.FillRectangle(brush, X1, Y1, 2, 2);
        public override string GetName() => "Point";
    }

    // Базові класи реалізують інтерфейси поведінки, тому їхні методи малювання
    // (default-реалізації інтерфейсів) доступні й складеним фігурам —
    // аналог виклику Show() базового класу при множинному успадкуванні в C++.
    public class LineShape : Shape, ILineBehavior
    {
        public LineShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush)
            => ((ILineBehavior)this).DrawLine(g, pen);
        public override string GetName() => "Line";
    }

    public class RectShape : Shape, IRectBehavior
    {
        public RectShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush)
            => ((IRectBehavior)this).DrawRectFilled(g, pen, brush);
        public override string GetName() => "Rectangle";
    }

    public class EllipseShape : Shape, IEllipseBehavior
    {
        public EllipseShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush)
            => ((IEllipseBehavior)this).DrawEllipseFilled(g, pen, brush);
        public override string GetName() => "Ellipse";
    }

    // "Множинне успадкування" в C#: Shape + інтерфейси ДВОХ базових поведінок
    // (еквівалент C++: class LineOOShape : public LineShape, public EllipseShape)
    public class LineWithCirclesShape : Shape, ILineBehavior, IEllipseBehavior
    {
        public LineWithCirclesShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            // Лінія — поведінка LineShape
            ((ILineBehavior)this).DrawLine(g, pen);

            // Кружечки на кінцях — поведінка EllipseShape
            ((IEllipseBehavior)this).DrawCircle(g, brush, X1, Y1, 3);
            ((IEllipseBehavior)this).DrawCircle(g, brush, X2, Y2, 3);
        }
        public override string GetName() => "LineWithCircles";
    }

    // Еквівалент C++: class CubeShape : public LineShape, public RectShape
    public class CubeWireframeShape : Shape, ILineBehavior, IRectBehavior
    {
        public CubeWireframeShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }
        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2), y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2), h = Math.Abs(Y1 - Y2);
            int offset = w / 3;

            // Грані — поведінка RectShape
            ((IRectBehavior)this).DrawRectOutline(g, pen, x, y, w, h);
            ((IRectBehavior)this).DrawRectOutline(g, pen, x + offset, y + offset, w, h);

            // Ребра — поведінка LineShape
            ILineBehavior line = (ILineBehavior)this;
            line.DrawLineSeg(g, pen, x, y, x + offset, y + offset);
            line.DrawLineSeg(g, pen, x + w, y, x + w + offset, y + offset);
            line.DrawLineSeg(g, pen, x, y + h, x + offset, y + h + offset);
            line.DrawLineSeg(g, pen, x + w, y + h, x + w + offset, y + h + offset);
        }
        public override string GetName() => "CubeWireframe";
    }
}
