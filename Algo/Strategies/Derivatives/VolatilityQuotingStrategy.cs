namespace StockSharp.Algo.Strategies.Derivatives
{
	using System;

	using Ecng.ComponentModel;

	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Messages;

	/// <summary>
	/// Option volatility quoting.
	/// </summary>
	public class VolatilityQuotingStrategy : BestByPriceQuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolatilityQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		/// <param name="ivRange">Volatility range.</param>
		public VolatilityQuotingStrategy(Sides quotingDirection, decimal quotingVolume, Range<decimal> ivRange)
			: base(quotingDirection, quotingVolume)
		{
			_ivRange = this.Param(nameof(IVRange), ivRange);
		}

		private readonly StrategyParam<Range<decimal>> _ivRange;

		/// <summary>
		/// Volatility range.
		/// </summary>
		public Range<decimal> IVRange
		{
			get => _ivRange.Value;
			set => _ivRange.Value = value;
		}

		/// <summary>
		/// <see cref="IBlackScholes"/>
		/// </summary>
		public IBlackScholes Model { get; set; }

        /// <inheritdoc />
        protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			var model = Model;

			if (model is null)
				return null;

			var minPrice = model.Premium(currentTime, IVRange.Min / 100);

			if (minPrice == null)
				return null;

			var maxPrice = model.Premium(currentTime, IVRange.Max / 100);

			if (maxPrice == null)
				return null;

			if (currentPrice == null || currentPrice < minPrice || currentPrice > maxPrice)
				return (minPrice + maxPrice) / 2;

			if (currentVolume != newVolume)
				return currentPrice;

			return null;
		}
	}
}