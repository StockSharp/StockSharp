namespace StockSharp.Samples.Advanced.MultiConnect;

using System.ComponentModel;
using System.Windows;

using Ecng.Common;
using Ecng.Configuration;

using StockSharp.BusinessEntities;
using StockSharp.Xaml;

public partial class MainWindow
{
	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		Title = Title.Put("Connections");

		ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		MainPanel.Close();

		base.OnClosing(e);
	}

	public static MainWindow Instance { get; private set; }

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();
	}
}