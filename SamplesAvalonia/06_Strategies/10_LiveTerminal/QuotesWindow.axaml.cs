namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;

using global::Avalonia.Controls;

using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Xaml.Grids.Avalonia;

public partial class QuotesWindow : Window, IDisposable
{
	private readonly MarketDepthControl _depth;
	private bool _disposed;

	public QuotesWindow()
	{
		InitializeComponent();
		_depth = this.FindControl<MarketDepthControl>(nameof(DepthControl));
	}

	public void Update(IOrderBookMessage depth)
	{
		if (!_disposed)
			_depth.UpdateDepth(depth);
	}

	public void ProcessOrder(Order order)
	{
		if (!_disposed)
			_depth.ProcessOrder(order, order.Price, order.Balance, order.State);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_depth.Clear();
	}
}
