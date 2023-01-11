namespace StockSharp.Algo.Strategies.Quoting
{
	using StockSharp.Messages;

	/// <summary>
	/// The strategy realizing volume quoting algorithm by the limited price.
	/// </summary>
	public class LimitQuotingStrategy : QuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LimitQuotingStrategy"/>.
		/// </summary>
		public LimitQuotingStrategy()
			: this(Sides.Buy, 1, 0)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LimitQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		/// <param name="limitPrice">The limited price for quoted orders.</param>
		public LimitQuotingStrategy(Sides quotingDirection, decimal quotingVolume, decimal limitPrice)
			: base(quotingDirection, quotingVolume)
		{
			_limitPrice = this.Param(nameof(LimitPrice), limitPrice);
		}

		private readonly StrategyParam<decimal> _limitPrice;

		/// <summary>
		/// The limited price for quoted orders.
		/// </summary>
		public decimal LimitPrice
		{
			get => _limitPrice.Value;
			set => _limitPrice.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? BestPrice => LimitPrice;
	}
}