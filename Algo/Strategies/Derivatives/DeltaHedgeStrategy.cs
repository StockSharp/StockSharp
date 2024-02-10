namespace StockSharp.Algo.Strategies.Derivatives
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Derivatives;

	/// <summary>
	/// The options delta hedging strategy.
	/// </summary>
	public class DeltaHedgeStrategy : HedgeStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DeltaHedgeStrategy"/>.
		/// </summary>
		/// <param name="blackScholes"><see cref="BasketBlackScholes"/>.</param>
		public DeltaHedgeStrategy(BasketBlackScholes blackScholes)
			: base(blackScholes)
		{
			_positionOffset = this.Param<decimal>(nameof(PositionOffset));
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
				return Enumerable.Empty<Order>();

			var diff = futurePosition.Value.Round() + PositionOffset;

			this.AddInfoLog("Delta total {0}, Futures position {1}, Directional position {2}, Difference in position {3}.", futurePosition, BlackScholes.UnderlyingAsset, PositionOffset, diff);

			if (diff == 0)
				return Enumerable.Empty<Order>();

			var side = diff > 0 ? Sides.Sell : Sides.Buy;
			var security = this.GetSecurity();

			var price = security.GetCurrentPrice(this, side);

			if (price == null)
				return Enumerable.Empty<Order>();

			return new[]
			{
				new Order
				{
					Side = side,
					Volume = diff.Abs(),
					Security = BlackScholes.UnderlyingAsset,
					Portfolio = Portfolio,
					Price = price.ApplyOffset(side, PriceOffset, security)
				}
			};
		}
	}
}