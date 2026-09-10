using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Lab5
{
    // Незалежний модуль-таблиця (аналог my_table з методички):
    // НЕ залежить від класів Shape та MyEditor — приймає дані у вигляді
    // простих кортежів, тому компонент можна використати в іншому проєкті
    // без жодних залежностей від редактора.
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
