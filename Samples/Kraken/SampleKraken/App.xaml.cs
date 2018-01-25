namespace SampleKraken
{
	using System.Windows;
	using System.Windows.Threading;

	public partial class App
	{
		private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(MainWindow, e.Exception.ToString());
			e.Handled = true;
		}
	}
}