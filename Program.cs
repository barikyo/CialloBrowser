using System;
using System.Windows.Forms;

namespace CialloBrowser
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //修复：使用 Framework 4.8 的传统启动方式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 启动主窗口
            Application.Run(new Form1());
        }
    }
}
