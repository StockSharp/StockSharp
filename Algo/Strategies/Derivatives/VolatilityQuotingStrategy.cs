namespace StockSharp.Algo.Strategies.Derivatives
{
	using System;

	using Ecng.ComponentModel;

	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Option volatility quoting.
	/// </summary>
	public class VolatilityQuotingStrategy : BestByPriceQuotingStrategy
	{
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
		private BlackScholes _bs;

		/// <summary>
		/// Initializes a new instance of the <see cref="VolatilityQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		/// <param name="ivRange">Volatility range.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public VolatilityQuotingStrategy(Sides quotingDirection, decimal quotingVolume, Range<decimal> ivRange, IExchangeInfoProvider exchangeInfoProvider)
			: base(quotingDirection, quotingVolume)
		{
			_exchangeInfoProvider = exchangeInfoProvider;
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

		/// <inheritdoc />
		public override Security Security
		{
			set
			{
				_bs = new BlackScholes(value, this, this, _exchangeInfoProvider);
				base.Security = value;
			}
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			var minPrice = _bs.Premium(currentTime, IVRange.Min / 100);

			if (minPrice == null)
				return null;

			var maxPrice = _bs.Premium(currentTime, IVRange.Max / 100);

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