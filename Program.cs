using System;
using System.Windows.Forms;

namespace CialloBrowser
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // 启动 Form1
            Application.Run(new Form1());
        }
    }

}
