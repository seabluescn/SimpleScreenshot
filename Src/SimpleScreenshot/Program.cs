using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {          
            Mutex mtx = new Mutex(true, "OnlyRunOneInstance", out bool isRuned);
            if (isRuned)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormMain());
                mtx.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("程序已启动！", "Simple Screenshot");
            }            
        }
    }
}
