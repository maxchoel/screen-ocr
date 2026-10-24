using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScreenOcrTool
{
    internal static class Program
    {
        private const string AppTitle = "屏幕识字";
        private static Mutex _instanceMutex;

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            _instanceMutex = new Mutex(true, @"Local\ScreenOcrTool.SingleInstance", out createdNew);
            if (!createdNew)
            {
                ActivateExistingWindow();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // PaddleOCR 的模型路径按当前目录解析，先把工作目录固定到程序所在目录。
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch
            {
                // 目录不可切换时仍可尝试按绝对路径识别。
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                GC.KeepAlive(_instanceMutex);
                try
                {
                    _instanceMutex.ReleaseMutex();
                }
                catch
                {
                    // 进程退出时由系统回收。
                }
                _instanceMutex.Dispose();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowUnexpectedError(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowUnexpectedError(e.ExceptionObject as Exception);
        }

        private static void ShowUnexpectedError(Exception exception)
        {
            string message = exception == null ? "未知错误" : exception.Message;
            MessageBox.Show(
                "程序遇到了一个意外错误：\r\n\r\n" + message,
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void ActivateExistingWindow()
        {
            IntPtr handle = FindWindow(null, AppTitle);
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, 9);
                SetForegroundWindow(handle);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
