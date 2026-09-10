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

            string[] groups = { "IM-051", "IM-052", "IM-053", "IM-054", "IM-055", "IM-056" };
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
                    Result = lbGroups.SelectedItem?.ToString() ?? "";
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
