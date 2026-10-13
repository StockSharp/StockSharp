namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;

using Avalonia.Controls;

using Ecng.Collections;

using StockSharp.BusinessEntities;
using StockSharp.Xaml;

public partial class MyTradesWindow : Window, IDisposable
{
	private readonly MyTradeGrid _grid;
	private bool _disposed;

	public MyTradesWindow()
	{
		InitializeComponent();
		_grid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
	}

	public void AddTrade(MyTrade trade)
	{
		if (!_disposed)
			_grid.Trades.TryAdd(trade);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_grid.Trades.Clear();
	}
}
