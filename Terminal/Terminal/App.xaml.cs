namespace Terminal
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;

    public partial class App
    {
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            ((Exception)args.ExceptionObject).ToTraceError();
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TraceLog.txt");
            var trl = new TextWriterTraceListener(File.CreateText(path));
            Trace.AutoFlush = true;
            Trace.Listeners.Add(trl);

            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += OnUnhandledException;

            try
            {
                var mnd = new MainWindow();
                MainWindow = mnd;
                this.MainWindow.Show();
            }
            catch (Exception ex)
            {
                ex.ToTraceError();
            }
        }
    }

}