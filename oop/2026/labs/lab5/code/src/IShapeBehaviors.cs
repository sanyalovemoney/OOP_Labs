using System;
using System.Drawing;

namespace Lab5.Shapes
{
    // C# не підтримує множинне успадкування КЛАСІВ, тому вимога методички
    // (LineOOShape : LineShape, EllipseShape та CubeShape : LineShape, RectShape)
    // реалізована через інтерфейси з default-реалізаціями (C# 8+).
    // Кожен інтерфейс несе поведінку малювання "свого" класу, а складена фігура
    // реалізує ДЕКІЛЬКА інтерфейсів одночасно — це C#-аналог множинного
    // успадкування: методи успадковуються від кількох базових типів.

    /// <summary>Поведінка LineShape: малювання відрізків прямих.</summary>
    public interface ILineBehavior
    {
        int X1 { get; }
        int Y1 { get; }
        int X2 { get; }
        int Y2 { get; }

        void DrawLine(Graphics g, Pen pen) => g.DrawLine(pen, X1, Y1, X2, Y2);

        void DrawLineSeg(Graphics g, Pen pen, int x1, int y1, int x2, int y2)
            => g.DrawLine(pen, x1, y1, x2, y2);
    }

    /// <summary>Поведінка EllipseShape: малювання еліпсів та кіл.</summary>
    public interface IEllipseBehavior
    {
        int X1 { get; }
        int Y1 { get; }
        int X2 { get; }
        int Y2 { get; }

        void DrawEllipseFilled(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2), y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2), h = Math.Abs(Y1 - Y2);
            g.FillEllipse(brush, x, y, w, h);
            g.DrawEllipse(pen, x, y, w, h);
        }

        void DrawCircle(Graphics g, Brush brush, int cx, int cy, int r)
            => g.FillEllipse(brush, cx - r, cy - r, 2 * r, 2 * r);
    }

    /// <summary>Поведінка RectShape: малювання прямокутників.</summary>
    public interface IRectBehavior
    {
        int X1 { get; }
        int Y1 { get; }
        int X2 { get; }
        int Y2 { get; }

        void DrawRectFilled(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2), y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2), h = Math.Abs(Y1 - Y2);
            g.FillRectangle(brush, x, y, w, h);
            g.DrawRectangle(pen, x, y, w, h);
        }

        void DrawRectOutline(Graphics g, Pen pen, int x, int y, int w, int h)
            => g.DrawRectangle(pen, x, y, w, h);
    }
}
