namespace StockSharp.Algo.Strategies;

using System;

using Ecng.Common;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

public partial class Strategy
{
	/// <summary>
	/// To create initialized object of buy order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyAtMarket(decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Buy, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object of sell order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellAtMarket(decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Sell, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for buy.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyAtLimit(decimal price, decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Buy, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for sell.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellAtLimit(decimal price, decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Sell, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To close open position by market (to register the order of the type <see cref="OrderTypes.Market"/>).
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If not passed the value from <see cref="Strategy.Security"/> will be obtain.</param>
	/// <param name="portfolio"><see cref="BusinessEntities.Portfolio"/>. If not passed the value from <see cref="Strategy.Portfolio"/> will be obtain.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The market order is not operable on all exchanges.
	/// </remarks>
	public Order ClosePosition(Security security = default, Portfolio portfolio = default)
	{
		var position = security is null ? Position : GetPositionValue(security, portfolio) ?? default;
		
		if (position == 0)
			return null;

		var volume = position.Abs();

		return position > 0 ? SellAtMarket(volume) : BuyAtMarket(volume);
	}
}
