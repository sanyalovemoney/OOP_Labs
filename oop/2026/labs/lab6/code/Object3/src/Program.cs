using System;
using System.Windows.Forms;

namespace Lab6.Object3
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Object3Form());
        }
    }
}
