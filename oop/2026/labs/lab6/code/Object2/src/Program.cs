using System;
using System.Windows.Forms;

namespace Lab6.Object2
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Object2Form());
        }
    }
}
