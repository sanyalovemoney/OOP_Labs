# Лабораторна робота №5: Багатовіконний інтерфейс та Singleton

## Опис
Ця робота присвячена впровадженню патерну Singleton для керування станом редактора, створенню додаткових вікон інтерфейсу та реалізації збереження даних у файл.

## Реалізація
Програма написана на C# (WinForms).

### 1. Патерн Singleton Меєрса (`MyEditor.cs`)
Клас `MyEditor` реалізований як **Singleton Меєрса** (варіант завдання: непарний номер у списку, Ж = 7 → «глобальний статичний об'єкт класу MyEditor у вигляді Singleton Меєрса»). C#-аналог static-локальної змінної з C++ `getInstance()` — це `Lazy<T>` у режимі `ExecutionAndPublication`: лінива потокобезпечна ініціалізація гарантується середовищем виконання.
- `GetShapes()` повертає `IReadOnlyList<Shape>` — зовнішній код не може змінити внутрішній список.
- `AddShape` і `DrawAll` синхронізовані через `lock` (ітерація йде по знімку списку).

### 2. Немодальне вікно таблиці (`MyTableForm.cs`)
Створено окреме вікно, що містить `ListView` з деталями всіх створених об'єктів (Назва, X1, Y1, X2, Y2). 
- Вікно є немодальним, що дозволяє користувачу одночасно працювати з редактором та переглядати список об'єктів.
- **Незалежний модуль** (вимога методички до `my_table`): форма не залежить від класів `Shape`/`MyEditor` — приймає дані як прості кортежі `(Name, X1, Y1, X2, Y2)`, тому компонент можна використати в іншому проєкті без жодних залежностей.
- **Автооновлення**: при кожному додаванні нового об'єкта рядок автоматично з'являється у відкритому вікні таблиці (вимога методички).

### 3. Запис та завантаження з файлу
- Збереження списку об'єктів у текстовий файл (`.txt`) з роздільниками-табуляціями (`File → Save`).
- **Бонус (п.3 методички)**: завантаження множини об'єктів з файлу (`File → Load`) — об'єкти відновлюються поліморфною фабрикою за іменем класу та відображаються і у головному вікні, і у таблиці.

### 4. «Множинне успадкування» складених фігур (C#-аналог)
Як і в лаб. №4, `LineWithCirclesShape` та `CubeWireframeShape` реалізують інтерфейси з default-реалізаціями `ILineBehavior + IEllipseBehavior` та `ILineBehavior + IRectBehavior` — C#-еквівалент множинного успадкування поведінок `LineShape`/`EllipseShape`/`RectShape` (файл `IShapeBehaviors.cs`).

---

## Вихідний код

### MyEditor.cs (Singleton)
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Lab5.Shapes;

namespace Lab5
{
    public class MyEditor
    {
        // Singleton Меєрса (C#-аналог): Lazy<T> у режимі ExecutionAndPublication
        // гарантує єдиний екземпляр із лінивою потокобезпечною ініціалізацією —
        // як static-локальна змінна у C++: static Singleton instance; return instance;
        // (Варіант: непарний номер у списку, Ж = 7 → Singleton Меєрса)
        private static readonly Lazy<MyEditor> _lazy =
            new Lazy<MyEditor>(() => new MyEditor());

        public static MyEditor Instance => _lazy.Value;

        private readonly object _sync = new object();
        private readonly List<Shape> _shapes = new List<Shape>();

        private MyEditor() { }

        public void AddShape(Shape shape)
        {
            lock (_sync)
            {
                _shapes.Add(shape);
            }
        }

        // Read-only view: callers cannot mutate the internal list
        public IReadOnlyList<Shape> GetShapes() => _shapes.AsReadOnly();

        public void DrawAll(Graphics g, Pen pen)
        {
            List<Shape> snapshot;
            lock (_sync)
            {
                snapshot = new List<Shape>(_shapes);
            }

            foreach (var shape in snapshot)
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

        public void SaveToFile(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (var s in GetShapes())
                {
                    sw.WriteLine($"{s.GetName()}\t{s.X1}\t{s.Y1}\t{s.X2}\t{s.Y2}");
                }
            }
        }

        // Бонус методички (п.3): завантаження множини об'єктів з файлу.
        // Формат рядка: Name<TAB>x1<TAB>y1<TAB>x2<TAB>y2
        public int LoadFromFile(string path)
        {
            var loaded = new List<Shape>();
            foreach (var line in File.ReadAllLines(path))
            {
                string[] p = line.Split('\t');
                if (p.Length < 5) continue;
                if (!int.TryParse(p[1], out int x1) || !int.TryParse(p[2], out int y1) ||
                    !int.TryParse(p[3], out int x2) || !int.TryParse(p[4], out int y2)) continue;

                Shape? shape = CreateShapeByName(p[0], x1, y1, x2, y2);
                if (shape != null) loaded.Add(shape);
            }

            lock (_sync)
            {
                _shapes.Clear();
                _shapes.AddRange(loaded);
            }
            return loaded.Count;
        }

        // Фабрика відновлення об'єктів за іменем класу (поліморфізм)
        private static Shape? CreateShapeByName(string name, int x1, int y1, int x2, int y2) => name switch
        {
            "Point" => new PointShape(x1, y1, x2, y2),
            "Line" => new LineShape(x1, y1, x2, y2),
            "Rectangle" => new RectShape(x1, y1, x2, y2),
            "Ellipse" => new EllipseShape(x1, y1, x2, y2),
            "LineWithCircles" => new LineWithCirclesShape(x1, y1, x2, y2),
            "CubeWireframe" => new CubeWireframeShape(x1, y1, x2, y2),
            _ => null
        };
    }
}
```

### MyTableForm.cs
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Lab5
{
    // Незалежний модуль-таблиця (аналог my_table з методички):
    // НЕ залежить від класів Shape та MyEditor — приймає дані у вигляді
    // простих кортежів, тому компонент можна використати в іншому проєкті
    public class MyTableForm : Form
    {
        private ListView _listView;

        public MyTableForm()
        {
            this.Text = "Shapes Table";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            _listView = new ListView
            {
                View = View.Details,
                Dock = DockStyle.Fill,
                FullRowSelect = true
            };

            _listView.Columns.Add("Name", 100);
            _listView.Columns.Add("X1", 50);
            _listView.Columns.Add("Y1", 50);
            _listView.Columns.Add("X2", 50);
            _listView.Columns.Add("Y2", 50);

            this.Controls.Add(_listView);
        }

        public void UpdateData(IEnumerable<(string Name, int X1, int Y1, int X2, int Y2)> rows)
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();
            foreach (var (name, x1, y1, x2, y2) in rows)
            {
                var item = new ListViewItem(name);
                item.SubItems.Add(x1.ToString());
                item.SubItems.Add(y1.ToString());
                item.SubItems.Add(x2.ToString());
                item.SubItems.Add(y2.ToString());
                _listView.Items.Add(item);
            }
            _listView.EndUpdate();
        }
    }
}
```
