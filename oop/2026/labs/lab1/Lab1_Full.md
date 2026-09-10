# Лабораторна робота №1: Базовий інтерфейс та модулі

## Опис
Ця лабораторна робота є початком проекту «Графічний редактор об'єктів». Основною метою було створення базового вікна програми з меню та реалізація двох модулів (діалогових вікон), які взаємодіють з головним вікном.

## Реалізація
Програма написана на C# (WinForms).

### 1. Головне вікно (`MainWindow.cs`)
Реалізує головне меню з розділами «Файл», «Роботи» та «Допомога». 
- **Робота 1**: Відкриває діалог вибору групи.
- **Робота 2**: Відкриває діалог введення тексту.
- **Допомога**: Виводить інформацію про студента.

### 2. Модуль 1 (`Module1Form.cs`)
Діалогове вікно, що містить `ListBox` зі списком груп факультету. Користувач обирає одну з груп, і результат передається назад у головне вікно.

### 3. Модуль 2 (`Module2Form.cs`)
Діалогове вікно для введення довільного тексту через `TextBox`. Результат відображається в центрі головного вікна.

---

## Вихідний код

### MainWindow.cs
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1
{
    public class MainWindow : Form
    {
        private MenuStrip menuStrip;
        private Label lblDisplayText;

        public MainWindow()
        {
            this.Text = "Лабораторна робота №1 (C#)";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            menuStrip = new MenuStrip();
            var menuWorks = new ToolStripMenuItem("Роботи");
            var menuHelp = new ToolStripMenuItem("Допомога");
            var menuFile = new ToolStripMenuItem("Файл");

            var itemWork1 = new ToolStripMenuItem("Робота 1", null, OnWork1Clicked);
            var itemWork2 = new ToolStripMenuItem("Робота 2", null, OnWork2Clicked);
            menuWorks.DropDownItems.Add(itemWork1);
            menuWorks.DropDownItems.Add(itemWork2);

            var itemAbout = new ToolStripMenuItem("Про програму...", null, OnAboutClicked);
            menuHelp.DropDownItems.Add(itemAbout);

            var itemExit = new ToolStripMenuItem("Вихід", null, (s, e) => Application.Exit());
            menuFile.DropDownItems.Add(itemExit);

            menuStrip.Items.Add(menuFile);
            menuStrip.Items.Add(menuWorks);
            menuStrip.Items.Add(menuHelp);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            lblDisplayText = new Label
            {
                Text = "Будь ласка, оберіть роботу в меню",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Regular)
            };
            this.Controls.Add(lblDisplayText);
            lblDisplayText.BringToFront();
        }

        private void OnWork1Clicked(object sender, EventArgs e)
        {
            using (var form = new Module1Form())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblDisplayText.Text = form.Result;
                }
            }
        }

        private void OnWork2Clicked(object sender, EventArgs e)
        {
            using (var form = new Module2Form())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblDisplayText.Text = form.Result;
                }
            }
        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Лабораторна робота №1\nСтудент: Мащута Олександр", "Про програму", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
```

### Module1Form.cs
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1
{
    public class Module1Form : Form
    {
        private ListBox lbGroups;
        private Button btnOk;
        private Button btnCancel;

        public string Result { get; private set; } = "";

        public Module1Form()
        {
            this.Text = "Робота 1 - Вибір групи";
            this.Size = new Size(250, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lbl = new Label
            {
                Text = "Оберіть групу свого факультету:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            lbGroups = new ListBox
            {
                Location = new Point(10, 30),
                Size = new Size(210, 80)
            };

            string[] groups = { "IM-51", "IM-52", "IM-53", "IM-54", "IM-55", "IM-о51" };
            foreach (var group in groups) lbGroups.Items.Add(group);
            if (lbGroups.Items.Count > 0) lbGroups.SelectedIndex = 0;

            btnOk = new Button
            {
                Text = "Так",
                Location = new Point(50, 120),
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Відміна",
                Location = new Point(120, 120),
                DialogResult = DialogResult.Cancel
            };

            btnOk.Click += (s, e) =>
            {
                if (lbGroups.SelectedItem != null)
                {
                    Result = lbGroups.SelectedItem.ToString();
                    this.Close();
                }
            };

            this.Controls.Add(lbl);
            this.Controls.Add(lbGroups);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
```

### Module2Form.cs
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1
{
    public class Module2Form : Form
    {
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;

        public string Result { get; private set; } = "";

        public Module2Form()
        {
            this.Text = "Робота 2 - Введення тексту";
            this.Size = new Size(250, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lbl = new Label
            {
                Text = "Введіть текст:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            txtInput = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(210, 20)
            };

            btnOk = new Button
            {
                Text = "Так",
                Location = new Point(50, 70),
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Відміна",
                Location = new Point(120, 70),
                DialogResult = DialogResult.Cancel
            };

            btnOk.Click += (s, e) =>
            {
                Result = txtInput.Text;
                this.Close();
            };

            this.Controls.Add(lbl);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
```
