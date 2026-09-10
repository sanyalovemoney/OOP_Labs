# Лабораторна робота №5: Багатовіконний інтерфейс та Singleton

## Опис
Ця робота присвячена впровадженню патерну Singleton для керування станом редактора, створенню додаткових вікон інтерфейсу та реалізації збереження даних у файл.

## Реалізація
Програма написана на C# (WinForms).

### 1. Патерн Singleton (`MyEditor.cs`)
Клас `MyEditor` тепер реалізований як Singleton. Це забезпечує єдину точку доступу до списку об'єктів з будь-якої частини програми (наприклад, з головного вікна або вікна таблиці).

### 2. Немодальне вікно таблиці (`MyTableForm.cs`)
Створено окреме вікно, що містить `ListView` з деталями всіх створених об'єктів (Назва, X1, Y1, X2, Y2). 
- Вікно є немодальним, що дозволяє користувачу одночасно працювати з редактором та переглядати список об'єктів.

### 3. Збереження у файл
Додано функцію збереження списку об'єктів у текстовий файл (`.txt`) з роздільниками-табуляціями.

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
        private static MyEditor _instance;
        private static readonly object _lock = new object();
        private List<Shape> _shapes = new List<Shape>();

        private MyEditor() { }

        public static MyEditor Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new MyEditor();
                    return _instance;
                }
            }
        }

        public void AddShape(Shape shape) => _shapes.Add(shape);
        public List<Shape> GetShapes() => _shapes;

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

        public void SaveToFile(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (var s in _shapes)
                {
                    sw.WriteLine($"{s.GetName()}\t{s.X1}\t{s.Y1}\t{s.X2}\t{s.Y2}");
                }
            }
        }
    }
}
```

### MyTableForm.cs
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Lab5.Shapes;

namespace Lab5
{
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

        public void UpdateData()
        {
            _listView.Items.Clear();
            foreach (var s in MyEditor.Instance.GetShapes())
            {
                var item = new ListViewItem(s.GetName());
                item.SubItems.Add(s.X1.ToString());
                item.SubItems.Add(s.Y1.ToString());
                item.SubItems.Add(s.X2.ToString());
                item.SubItems.Add(s.Y2.ToString());
                _listView.Items.Add(item);
            }
        }
    }
}
```
