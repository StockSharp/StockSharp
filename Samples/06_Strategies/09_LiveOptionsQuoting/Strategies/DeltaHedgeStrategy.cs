namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
public class DeltaHedgeStrategy : HedgeStrategy
{
	private decimal _lastDelta;

	/// <summary>
	/// Initializes a new instance of the <see cref="DeltaHedgeStrategy"/>.
	/// </summary>
	/// <param name="blackScholes">Portfolio model for calculating the Greeks.</param>
	public DeltaHedgeStrategy(BasketBlackScholes blackScholes)
		: base(blackScholes)
	{
		_positionOffset = Param(nameof(PositionOffset), 0m)
			.SetDisplay("Position Offset", "Shift in position for underlying asset, allowing not to hedge part of the options position", "Delta Hedging");
	}

	private readonly StrategyParam<decimal> _positionOffset;

	/// <summary>
	/// Shift in position for underlying asset, allowing not to hedge part of the options position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionOffsetKey,
		Description = LocalizedStrings.PositionOffsetDescKey,
		GroupName = "Delta Hedging",
		Order = 0)]
	public decimal PositionOffset
	{
		get => _positionOffset.Value;
		set => _positionOffset.Value = value;
	}

	/// <inheritdoc />
	protected override IEnumerable<Order> GetReHedgeOrders(DateTimeOffset currentTime)
	{
		// Calculate the portfolio delta
		var portfolioDelta = BlackScholes.Delta(currentTime);

		if (portfolioDelta == null)
		{
			LogWarning("Unable to calculate portfolio delta");
			return [];
		}

		// Round the delta and apply the offset
		var targetDelta = portfolioDelta.Value.Round() + PositionOffset;

		// Get the underlying asset for hedging
		var underlyingAsset = BlackScholes.UnderlyingAsset;
		if (underlyingAsset == null)
		{
			LogError("Underlying asset not specified");
			return [];
		}

		// Get current position in the underlying asset
		var currentPosition = GetPositionValue(underlyingAsset, Portfolio) ?? 0m;

		// Calculate the difference between current position and target delta
		var positionDiff = targetDelta - currentPosition;

		// Log details
		LogInfo("Delta calculation: Portfolio Delta = {0}, Target Delta = {1}, Current Position = {2}, Difference = {3}",
			portfolioDelta.Value, targetDelta, currentPosition, positionDiff);

		// Store the last calculated delta
		_lastDelta = portfolioDelta.Value;

		// Check if rehedging is necessary based on the threshold
		if (Math.Abs(positionDiff) < HedgingThreshold)
		{
			LogInfo("Position difference {0} is below threshold {1}, no rehedging needed", positionDiff, HedgingThreshold);
			return [];
		}

		// Determine trade direction
		var side = positionDiff > 0 ? Sides.Buy : Sides.Sell;
		var volume = Math.Abs(positionDiff);

		// Get current market price
		if (underlyingAsset.GetCurrentPrice(this, side) is not decimal price)
		{
			LogWarning("Unable to determine price for {0}", underlyingAsset.Id);
			return [];
		}

		// Apply price offset based on side
		var adjustedPrice = price.ApplyOffset(side, PriceOffset, underlyingAsset);

		// Create the rehedging order
		var order = new Order
		{
			Security = underlyingAsset,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Price = adjustedPrice,
			Type = OrderTypes.Limit
		};

		return [order];
	}

	/// <summary>
	/// Get the current portfolio delta.
	/// </summary>
	/// <returns>The last calculated portfolio delta.</returns>
	public decimal GetCurrentDelta() => _lastDelta;
}