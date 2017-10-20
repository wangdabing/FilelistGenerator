using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FilelistGenerator
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            Application.ApplicationExit += Application_ApplicationExit;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "出错了~", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(Environment.ExitCode);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            var message = ex == null ? "未知错误" : ex.Message;
            MessageBox.Show(message, "出错了~", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
