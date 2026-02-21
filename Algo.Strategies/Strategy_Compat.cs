namespace StockSharp.Algo.Strategies;

using System;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

partial class Strategy
{
	/// <summary>
	/// Place a buy stop order at the given price.
	/// </summary>
	public Order BuyStop(decimal volume, decimal price, Security security = null)
	{
		var order = CreateOrder(Sides.Buy, price, volume);

		if (security != null)
			order.Security = security;

		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Place a buy stop order at the given price using default volume.
	/// </summary>
	public Order BuyStop(decimal price)
		=> BuyStop(Volume, price);

	/// <summary>
	/// Place a sell stop order at the given price.
	/// </summary>
	public Order SellStop(decimal volume, decimal price, Security security = null)
	{
		var order = CreateOrder(Sides.Sell, price, volume);

		if (security != null)
			order.Security = security;

		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Place a sell stop order at the given price using default volume.
	/// </summary>
	public Order SellStop(decimal price)
		=> SellStop(Volume, price);

	/// <summary>
	/// Average entry price of the current position (stub: returns last trade price).
	/// </summary>
	public decimal PositionPrice
		=> this.GetSecurityValue<decimal>(Security, Level1Fields.LastTradePrice);

	/// <summary>
	/// Draw a line on the chart (no-op in backtest).
	/// </summary>
	protected void DrawLine(DateTimeOffset time1, decimal price1, DateTimeOffset time2, decimal price2, int thickness = 1)
	{
	}
}
