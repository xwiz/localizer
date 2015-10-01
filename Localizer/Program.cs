using System;
using System.Windows.Forms;

namespace Localizer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Localizer.myLocalizer = new Localizer("en");
            Localizer.myLocalizer = Localizer.myLocalizer.Deserialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
