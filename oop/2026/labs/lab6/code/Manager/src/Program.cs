using System;
using System.Windows.Forms;

namespace Lab6.Manager
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ManagerForm());
        }
    }
}
