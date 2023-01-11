namespace StockSharp.Algo.Strategies.Quoting
{
	using System;
	
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The quoting by the market price.
	/// </summary>
	public class MarketQuotingStrategy : BestByPriceQuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketQuotingStrategy"/>.
		/// </summary>
		public MarketQuotingStrategy()
			: this(Sides.Buy, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		public MarketQuotingStrategy(Sides quotingDirection, decimal quotingVolume)
			: base(quotingDirection, quotingVolume)
		{
			_priceType = this.Param(nameof(PriceType), MarketPriceTypes.Following);
			_priceOffset = this.Param(nameof(PriceOffset), new Unit());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketQuotingStrategy"/>.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		/// <param name="bestPriceOffset">The shift from the best price, on which the quoted order can be changed.</param>
		/// <param name="priceOffset">The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).</param>
		/// <returns>Strategy.</returns>
		public MarketQuotingStrategy(Order order, Unit bestPriceOffset, Unit priceOffset)
			: base(order, bestPriceOffset)
		{
			_priceType = this.Param(nameof(PriceType), MarketPriceTypes.Following);
			_priceOffset = this.Param(nameof(PriceOffset), priceOffset);

			UseLastTradePrice = false;
		}

		private readonly StrategyParam<MarketPriceTypes> _priceType;

		/// <summary>
		/// The market price type. The default value is <see cref="MarketPriceTypes.Following"/>.
		/// </summary>
		public MarketPriceTypes PriceType
		{
			get => _priceType.Value;
			set => _priceType.Value = value;
		}

		private readonly StrategyParam<Unit> _priceOffset;

		/// <summary>
		/// The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).
		/// </summary>
		public Unit PriceOffset
		{
			get => _priceOffset.Value;
			set => _priceOffset.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? BestPrice
		{
			get
			{
				Unit newPrice;
				Unit offset;

				switch (PriceType)
				{
					case MarketPriceTypes.Opposite:
					{
						var quote = GetFilteredQuotes(QuotingDirection.Invert())?.FirstOr();

						newPrice = quote?.Price ?? LastTradePrice;

						offset = -PriceOffset;

						break;
					}
					case MarketPriceTypes.Following:
					{
						newPrice = base.BestPrice;
						offset = PriceOffset;
						break;
					}
					case MarketPriceTypes.Middle:
						var bestBid = GetFilteredQuotes(Sides.Buy)?.FirstOr();
						var bestAsk = GetFilteredQuotes(Sides.Sell)?.FirstOr();
						newPrice = bestBid != null && bestAsk != null
							? bestBid.Value.Price + (bestAsk.Value.Price - bestBid.Value.Price) / 2m
							: null;
						offset = null;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (newPrice == null)
					return null;

				newPrice.SetSecurity(Security);

				if (offset != null)
					newPrice = newPrice.ApplyOffset(QuotingDirection, offset, Security);

				return (decimal)newPrice;
			}
		}
	}
}
