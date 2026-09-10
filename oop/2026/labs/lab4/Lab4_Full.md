# Лабораторна робота №4: Рефакторинг та складні об'єкти

## Опис
Ця робота присвячена рефакторингу архітектури програми для відокремлення логіки керування об'єктами від інтерфейсу користувача, а також реалізації складних графічних об'єктів.

## Реалізація
Програма написана на C# (WinForms).

### 1. Рефакторинг: Клас `MyEditor`
Всю логіку зберігання та малювання об'єктів перенесено з `MainWindow` у спеціальний клас `MyEditor`.
- `MyEditor` містить список усіх створених фігур.
- Метод `DrawAll(Graphics g, Pen pen)` ітерує по всіх об'єктах та викликає їхні методи малювання. Колір заповнення обирається всередині редактора методом `GetFillBrush` — зовнішній `Brush` не мутується.
- Створення фігур у `MainWindow` централізоване у фабричному методі `CreateShape` (switch-вираз) замість дубльованих switch-блоків.

### 2. Складні об'єкти та множинне успадкування (C#-аналог)
Методичка вимагає множинного успадкування (`LineOOShape : LineShape, EllipseShape`, `CubeShape : LineShape, RectShape`), яке C# **не підтримує на рівні класів**. Згідно з приміткою силабуса (дозволено використовувати інші мови з виконанням функціональних вимог та архітектури рішення), множинне успадкування реалізовано **інтерфейсами з default-реалізаціями (C# 8+)** — прямим C#-аналогом:

- Інтерфейси поведінки `ILineBehavior`, `IEllipseBehavior`, `IRectBehavior` (файл `IShapeBehaviors.cs`) містять **готові методи малювання** (default interface methods) — поведінку класів `LineShape`, `EllipseShape`, `RectShape` відповідно.
- Базові класи реалізують свої інтерфейси, тому вся поведінка має єдине джерело.
- **LineWithCirclesShape** : `Shape, ILineBehavior, IEllipseBehavior` — успадковує поведінку ДВОХ базових типів одночасно (еквівалент `LineShape + EllipseShape`): лінія малюється методом `DrawLine` (поведінка LineShape), кружечки — `DrawCircle` (поведінка EllipseShape).
- **CubeWireframeShape** : `Shape, ILineBehavior, IRectBehavior` — еквівалент `LineShape + RectShape`: грані малюються `DrawRectOutline` (поведінка RectShape), ребра — `DrawLineSeg` (поведінка LineShape).

Виклик default-методу інтерфейсу виконується через явне приведення `((ILineBehavior)this).DrawLine(...)` — це C#-механізм розв'язання «ромбічної» неоднозначності (порівняння з віртуальними базовими класами C++ — див. контрольні запитання).

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

        public void DrawAll(Graphics g, Pen pen)
        {
            foreach (var shape in _shapes)
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

        public void Clear()
        {
            _shapes.Clear();
        }
    }
}
```

### IShapeBehaviors.cs (інтерфейси поведінки — C#-аналог множинного успадкування)
```csharp
using System;
using System.Drawing;

namespace Lab4.Shapes
{
    // Кожен інтерфейс несе поведінку малювання "свого" класу
    // (default interface methods, C# 8+). Складена фігура реалізує
    // ДЕКІЛЬКА інтерфейсів одночасно — еквівалент множинного успадкування.

    public interface ILineBehavior
    {
        int X1 { get; } int Y1 { get; } int X2 { get; } int Y2 { get; }

        void DrawLine(Graphics g, Pen pen) => g.DrawLine(pen, X1, Y1, X2, Y2);

        void DrawLineSeg(Graphics g, Pen pen, int x1, int y1, int x2, int y2)
            => g.DrawLine(pen, x1, y1, x2, y2);
    }

    public interface IEllipseBehavior
    {
        int X1 { get; } int Y1 { get; } int X2 { get; } int Y2 { get; }

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

    public interface IRectBehavior
    {
        int X1 { get; } int Y1 { get; } int X2 { get; } int Y2 { get; }

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
```

### CompositeShapes.cs
```csharp
using System;
using System.Drawing;

namespace Lab4.Shapes
{
    // "Множинне успадкування" в C#: клас успадковує Shape і одночасно
    // реалізує інтерфейси ДВОХ базових поведінок (LineShape + EllipseShape).
    // Еквівалент C++: class LineOOShape : public LineShape, public EllipseShape
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

    // Еквівалент C++: class CubeShape : public LineShape, public RectShape
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
```
