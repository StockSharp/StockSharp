namespace StockSharp.Samples.Strategies.LiveTerminal;

using StockSharp.Algo.Strategies;
using StockSharp.Xaml;

public partial class StrategyEditWindow
{
	public StrategyEditWindow()
	{
		InitializeComponent();

		SettingsGrid.SecurityProvider = MainWindow.Instance.Connector;
		SettingsGrid.Portfolios = new PortfolioDataSource(MainWindow.Instance.Connector);
	}

	public Strategy Strategy
	{
		get => (Strategy)SettingsGrid.SelectedObject;
		set => SettingsGrid.SelectedObject = value;
	}
}