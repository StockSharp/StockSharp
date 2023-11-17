namespace StockSharp.Algo.Strategies.Quoting
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The quoting by the best price. For this quoting the shift from the best price <see cref="BestPriceOffset"/> is specified, on which quoted order can be changed.
	/// </summary>
	public class BestByPriceQuotingStrategy : QuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BestByPriceQuotingStrategy"/>.
		/// </summary>
		public BestByPriceQuotingStrategy()
			: this(Sides.Buy, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BestByPriceQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		public BestByPriceQuotingStrategy(Sides quotingDirection, decimal quotingVolume)
			: base(quotingDirection, quotingVolume)
		{
			_bestPriceOffset = this.Param(nameof(BestPriceOffset), new Unit());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BestByPriceQuotingStrategy"/>.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		/// <param name="bestPriceOffset">The shift from the best price, on which quoted order can be changed.</param>
		public BestByPriceQuotingStrategy(Order order, Unit bestPriceOffset)
			: base(order)
		{
			_bestPriceOffset = this.Param(nameof(BestPriceOffset), bestPriceOffset);
		}

		private readonly StrategyParam<Unit> _bestPriceOffset;

		/// <summary>
		/// The shift from the best price, on which quoted order can be changed.
		/// </summary>
		public Unit BestPriceOffset
		{
			get => _bestPriceOffset.Value;
			set => _bestPriceOffset.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentOrderPrice, decimal? currentOrderVolume, decimal newVolume)
		{
			var preferrableQuotingPrice = BestPrice;

			if (preferrableQuotingPrice == null)
				return null;

			if(currentOrderPrice == null)
				return preferrableQuotingPrice;

			var diff = Math.Abs(currentOrderPrice.Value - preferrableQuotingPrice.Value);

			if(diff > 0m && diff >= BestPriceOffset)
				return preferrableQuotingPrice;

			if (currentOrderVolume != newVolume)
				return preferrableQuotingPrice;

			return null;
		}
	}
}