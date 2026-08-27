namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;

using global::Avalonia.Controls;

using Ecng.Collections;

using StockSharp.BusinessEntities;
using StockSharp.Xaml.Grids.Avalonia;

public partial class PortfoliosWindow : Window, IDisposable
{
	private readonly PortfolioGrid _grid;
	private bool _disposed;

	public PortfoliosWindow()
	{
		InitializeComponent();
		_grid = this.FindControl<PortfolioGrid>(nameof(PortfolioGrid));
	}

	public void AddPosition(Position position)
	{
		if (!_disposed)
			_grid.Positions.TryAdd(position);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_grid.Positions.Clear();
	}
}
