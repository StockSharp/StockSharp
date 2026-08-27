namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;

using global::Avalonia.Controls;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml.Grids.Avalonia;

public partial class OrdersWindow : Window, IDisposable
{
	private readonly IConnector _connector;
	private readonly OrderGrid _grid;
	private bool _disposed;

	public OrdersWindow(IConnector connector)
	{
		_connector = connector ?? throw new ArgumentNullException(nameof(connector));
		InitializeComponent();
		_grid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_grid.OrderCanceling += OnOrderCanceling;
	}

	public void AddOrder(Order order)
	{
		if (!_disposed)
			_grid.Orders.TryAdd(order);
	}

	public void AddRegistrationFail(OrderFail fail)
	{
		if (!_disposed)
			_grid.AddRegistrationFail(fail);
	}

	private void OnOrderCanceling(Order order)
	{
		if (!_disposed)
			_connector.CancelOrder(order);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_grid.OrderCanceling -= OnOrderCanceling;
		_grid.Orders.Clear();
	}
}
