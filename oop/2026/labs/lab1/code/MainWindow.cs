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

            // Setup Menu
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

            // Display Label
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

        private void OnWork1Clicked(object? sender, EventArgs e)
        {
            using (var form = new Module1Form())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblDisplayText.Text = form.Result;
                }
            }
        }

        private void OnWork2Clicked(object? sender, EventArgs e)
        {
            using (var form = new Module2Form())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblDisplayText.Text = form.Result;
                }
            }
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            MessageBox.Show("Лабораторна робота №1\nСтудент: Мащута Олександр", "Про програму", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
