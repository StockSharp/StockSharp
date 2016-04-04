#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleChart.SampleChartPublic
File: App.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleChart
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

		private void App_OnStartup(object sender, StartupEventArgs e)
		{
			//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.Office2016BlackName;
			//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.Office2010BlackName;
			//DevExpress.Xpf.Core.ThemeManager.ApplicationThemeName = DevExpress.Xpf.Core.Theme.MetropolisDarkName;
		}
	}
}