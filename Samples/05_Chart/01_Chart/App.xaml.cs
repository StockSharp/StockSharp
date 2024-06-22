namespace StockSharp.Samples.Chart;

using System.Windows;
using System.Windows.Threading;

public partial class App
{
	private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show(MainWindow, e.Exception.ToString());
		e.Handled = true;
	}

	private void App_OnStartup(object sender, StartupEventArgs e)
	{
		//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.Office2016BlackName;
		//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.Office2010BlackName;
		//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.MetropolisDarkName;
	}
}