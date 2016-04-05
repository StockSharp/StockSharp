using System.Windows;
using System.Windows.Threading;

namespace SampleChartActiveOrders {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(MainWindow, e.Exception.ToString());
			e.Handled = true;
		}

		private void App_OnStartup(object sender, StartupEventArgs e)
		{
		}
	}
}
