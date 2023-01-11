namespace StockSharp.Algo.Strategies.Quoting
{
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The quoting by the last trade price.
	/// </summary>
	public class LastTradeQuotingStrategy : BestByPriceQuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LastTradeQuotingStrategy"/>.
		/// </summary>
		public LastTradeQuotingStrategy()
			: this(Sides.Buy, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LastTradeQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		public LastTradeQuotingStrategy(Sides quotingDirection, decimal quotingVolume)
			: base(quotingDirection, quotingVolume)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LastTradeQuotingStrategy"/>.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		/// <param name="bestPriceOffset">The shift from the best price, on which the quoted order can be changed.</param>
		/// <returns>Strategy.</returns>
		public LastTradeQuotingStrategy(Order order, Unit bestPriceOffset)
			: base(order, bestPriceOffset)
		{
		}

		/// <inheritdoc />
		protected override decimal? BestPrice => LastTradePrice;
	}
}