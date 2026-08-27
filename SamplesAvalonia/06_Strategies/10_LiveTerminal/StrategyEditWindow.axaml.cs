namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Xaml.PropertyGrid.Avalonia;

public partial class StrategyEditWindow : Window, IDisposable
{
	private readonly PropertyGridEx _settingsGrid;
	private bool _disposed;

	public StrategyEditWindow(ISecurityProvider securityProvider, PortfolioDataSource portfolios)
	{
		InitializeComponent();
		_settingsGrid = this.FindControl<PropertyGridEx>(nameof(SettingsGrid));
		_settingsGrid.SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		_settingsGrid.Portfolios = portfolios ?? throw new ArgumentNullException(nameof(portfolios));
	}

	public Strategy Strategy
	{
		get => _settingsGrid.SelectedObject as Strategy;
		set => _settingsGrid.SelectedObject = value ?? throw new ArgumentNullException(nameof(value));
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
		=> Close(false);

	private void OnOkClick(object sender, RoutedEventArgs e)
		=> Close(true);

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_settingsGrid.SelectedObject = null;
		_settingsGrid.SecurityProvider = null;
		_settingsGrid.Portfolios = null;
	}
}
