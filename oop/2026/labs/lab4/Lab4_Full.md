# Лабораторна робота №4: Рефакторинг та складні об'єкти

## Опис
Ця робота присвячена рефакторингу архітектури програми для відокремлення логіки керування об'єктами від інтерфейсу користувача, а також реалізації складних графічних об'єктів.

## Реалізація
Програма написана на C# (WinForms).

### 1. Рефакторинг: Клас `MyEditor`
Всю логіку зберігання та малювання об'єктів перенесено з `MainWindow` у спеціальний клас `MyEditor`.
- `MyEditor` містить список усіх створених фігур.
- Метод `DrawAll` ітерує по всіх об'єктах та викликає їхні методи малювання.

### 2. Складні об'єкти та імітація множинного успадкування
Оскільки C# не підтримує множинне успадкування класів, реалізація складних об'єктів виконана через композицію та перевизначення методів:

- **LineWithCirclesShape**: Поєднує властивості лінії та еліпса. Малює відрізок, а на його кінцях — маленькі заповнені кола.
- **CubeWireframeShape**: Створює каркас куба. Малює дві прямокутники (передню та задню грані) та з'єднує їх чотирма лініями.

### 3. Оновлення інтерфейсу
- Toolbar розширено до 6 кнопок (додано «LineWithCircles» та «Cube»).
- Menu також оновлено для підтримки всіх типів об'єктів.

---

## Вихідний код

### MyEditor.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using Lab4.Shapes;

namespace Lab4
{
    public class MyEditor
    {
        private List<Shape> _shapes = new List<Shape>();

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);
        }

        public void DrawAll(Graphics g, Pen pen, Brush brush)
        {
            foreach (var shape in _shapes)
            {
                if (shape is RectShape) brush = Brushes.Orange;
                else if (shape is EllipseShape) brush = Brushes.White;
                else if (shape is LineWithCirclesShape) brush = Brushes.Yellow;
                else if (shape is CubeWireframeShape) brush = Brushes.Cyan;
                else brush = Brushes.LightGray;

                shape.Draw(g, pen, brush);
            }
        }
    }
}
```

### CompositeShapes.cs
```csharp
using System;
using System.Drawing;

namespace Lab4.Shapes
{
    public class LineWithCirclesShape : Shape
    {
        public LineWithCirclesShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            g.DrawLine(pen, X1, Y1, X2, Y2);
            int radius = 3;
            g.FillEllipse(brush, X1 - radius, Y1 - radius, radius * 2, radius * 2);
            g.FillEllipse(brush, X2 - radius, Y2 - radius, radius * 2, radius * 2);
        }
    }

    public class CubeWireframeShape : Shape
    {
        public CubeWireframeShape(int x1, int y1, int x2, int y2) : base(x1, y1, x2, y2) { }

        public override void Draw(Graphics g, Pen pen, Brush brush)
        {
            int x = Math.Min(X1, X2), y = Math.Min(Y1, Y2);
            int w = Math.Abs(X1 - X2), h = Math.Abs(Y1 - Y2);
            int offset = w / 3;

            g.DrawRectangle(pen, x, y, w, h);
            g.DrawRectangle(pen, x + offset, y + offset, w, h);
            g.DrawLine(pen, x, y, x + offset, y + offset);
            g.DrawLine(pen, x + w, y, x + w + offset, y + offset);
            g.DrawLine(pen, x, y + h, x + offset, y + h + offset);
            g.DrawLine(pen, x + w, y + h, x + w + offset, y + h + offset);
        }
    }
}
```
