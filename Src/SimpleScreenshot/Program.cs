using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    static class Program
    {
        // 外部函数声明
        [DllImport("user32.dll")]
        private static extern void SetProcessDPIAware();

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {          
            Mutex mtx = new Mutex(true, "OnlyRunOneInstance", out bool isRuned);
            if (isRuned)
            {
                SetProcessDPIAware();

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

    /*  SetProcessDPIAware是Vista以上才有的函数，直接调用会使得程序不兼容XP,需兼容XP的话按如下所示间接调用
               
            IntPtr hUser32 = GetModuleHandle("user32.dll");
            IntPtr addrSetProcessDPIAware = GetProcAddress(hUser32, "SetProcessDPIAware");
            if (addrSetProcessDPIAware != IntPtr.Zero)
            {
                FarProc SetProcessDPIAware = (FarProc)Marshal.GetDelegateForFunctionPointer(addrSetProcessDPIAware, typeof(FarProc));
                SetProcessDPIAware();
            }
        
        两个引用：
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string name);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] // 这个函数只能接受ASCII，所以一定要设置CharSet = CharSet.Ansi，不然会失败
        private static extern IntPtr GetProcAddress(IntPtr hmod, string name);   

     */
}
