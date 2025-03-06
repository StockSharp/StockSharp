namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Derivatives;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// The options delta hedging strategy.
/// </summary>
[Obsolete("Child strategies no longer supported.")]
public class DeltaHedgeStrategy : HedgeStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DeltaHedgeStrategy"/>.
	/// </summary>
	/// <param name="blackScholes"><see cref="BasketBlackScholes"/>.</param>
	public DeltaHedgeStrategy(BasketBlackScholes blackScholes)
		: base(blackScholes)
	{
		_positionOffset = Param<decimal>(nameof(PositionOffset));
	}

	private readonly StrategyParam<decimal> _positionOffset;

	/// <summary>
	/// Shift in position for underlying asset, allowing not to hedge part of the options position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionOffsetKey,
		Description = LocalizedStrings.PositionOffsetDescKey,
		GroupName = LocalizedStrings.HedgingKey,
		Order = 0)]
	public decimal PositionOffset
	{
		get => _positionOffset.Value;
		set => _positionOffset.Value = value;
	}

	/// <inheritdoc />
	protected override IEnumerable<Order> GetReHedgeOrders(DateTimeOffset currentTime)
	{
		var futurePosition = BlackScholes.Delta(currentTime);

		if (futurePosition == null)
			return [];

		var diff = futurePosition.Value.Round() + PositionOffset;

		LogInfo("Delta total {0}, Futures position {1}, Directional position {2}, Difference in position {3}.", futurePosition, BlackScholes.UnderlyingAsset, PositionOffset, diff);

		if (diff == 0)
			return [];

		var side = diff > 0 ? Sides.Sell : Sides.Buy;
		var security = GetSecurity();

		var price = security.GetCurrentPrice(this, side);

		if (price == null)
			return [];

		return
		[
			new Order
			{
				Side = side,
				Volume = diff.Abs(),
				Security = BlackScholes.UnderlyingAsset,
				Portfolio = Portfolio,
				Price = price.ApplyOffset(side, PriceOffset, security)
			}
		];
	}
}