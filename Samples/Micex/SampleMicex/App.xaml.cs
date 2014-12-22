namespace SampleMicex
{
	using System.Windows;
	using System.Windows.Threading;

	public partial class App
	{
		private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if(MainWindow == null)
			{
				MessageBox.Show(e.Exception.ToString());
			}
			else
			{
				MessageBox.Show(MainWindow, e.Exception.ToString());
			}


			e.Handled = true;
		}
	}
}