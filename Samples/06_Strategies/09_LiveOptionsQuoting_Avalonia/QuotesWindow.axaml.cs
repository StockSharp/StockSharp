namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using System;

using Avalonia.Controls;

using StockSharp.Messages;
using StockSharp.Xaml;

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

	public void ProcessOrder(StockSharp.BusinessEntities.Order order)
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
