namespace StockSharp.Algo.Strategies.Derivatives
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The options delta hedging strategy.
	/// </summary>
	public class DeltaHedgeStrategy : HedgeStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DeltaHedgeStrategy"/>.
		/// </summary>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public DeltaHedgeStrategy(IExchangeInfoProvider exchangeInfoProvider)
			: base(exchangeInfoProvider)
		{
			_positionOffset = new StrategyParam<decimal>(this, nameof(PositionOffset));
		}

		private readonly StrategyParam<decimal> _positionOffset;

		/// <summary>
		/// Shift in position for underlying asset, allowing not to hedge part of the options position.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1245Key,
			Description = LocalizedStrings.Str1246Key,
			GroupName = LocalizedStrings.Str1244Key,
			Order = 0)]
		public decimal PositionOffset
		{
			get => _positionOffset.Value;
			set => _positionOffset.Value = value;
		}

		/// <inheritdoc />
		protected override IEnumerable<Order> GetReHedgeOrders()
		{
			var futurePosition = BlackScholes.Delta(CurrentTime);

			if (futurePosition == null)
				return Enumerable.Empty<Order>();

			var diff = futurePosition.Value.Round() + PositionOffset;

			this.AddInfoLog(LocalizedStrings.Str1247Params, futurePosition, BlackScholes.UnderlyingAsset, PositionOffset, diff);

			if (diff == 0)
				return Enumerable.Empty<Order>();

			var dir = diff > 0 ? Sides.Sell : Sides.Buy;

			var price = Security.GetCurrentPrice(this, dir);

			if (price == null)
				return Enumerable.Empty<Order>();

			return new[]
			{
				new Order
				{
					Direction = dir,
					Volume = diff.Abs(),
					Security = BlackScholes.UnderlyingAsset,
					Portfolio = Portfolio,
					Price = price.ApplyOffset(dir, PriceOffset, Security)
				}
			};
		}
	}
}