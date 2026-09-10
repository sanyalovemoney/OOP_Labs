using System;
using System.Drawing;

namespace Lab4.Shapes
{
    // "Множинне успадкування" в C#: клас успадковує Shape і одночасно
    // реалізує ІНТЕРФЕЙСИ ДВОХ базових поведінок (LineShape + EllipseShape),
    // отримуючи готові методи малювання з обох (default interface methods).
    // Це еквівалент C++-оголошення:
    //   class LineOOShape : public LineShape, public EllipseShape { ... };

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
    }

    // Еквівалент C++-оголошення:
    //   class CubeShape : public LineShape, public RectShape { ... };

    public class CubeWireframeShape : Shape, ILineBehavior, IRectBehavior
    {
        public CubeWireframeShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2), y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2), h = Math.Abs(Y1 - Y2);
            int offset = w / 3;

            // Передня та задня грані — поведінка RectShape
            ((IRectBehavior)this).DrawRectOutline(g, pen, x, y, w, h);
            ((IRectBehavior)this).DrawRectOutline(g, pen, x + offset, y + offset, w, h);

            // З'єднувальні ребра — поведінка LineShape
            ILineBehavior line = (ILineBehavior)this;
            line.DrawLineSeg(g, pen, x, y, x + offset, y + offset);
            line.DrawLineSeg(g, pen, x + w, y, x + w + offset, y + offset);
            line.DrawLineSeg(g, pen, x, y + h, x + offset, y + h + offset);
            line.DrawLineSeg(g, pen, x + w, y + h, x + w + offset, y + h + offset);
        }
    }
}
