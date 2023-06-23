namespace StockSharp.Algo.Strategies.Derivatives
{
	using System;

	using Ecng.ComponentModel;

	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Messages;

	/// <summary>
	/// Option theoretical price quoting.
	/// </summary>
	public class TheorPriceQuotingStrategy : BestByPriceQuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TheorPriceQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		/// <param name="theorPriceOffset">Theoretical price offset.</param>
		public TheorPriceQuotingStrategy(Sides quotingDirection, decimal quotingVolume, Range<Unit> theorPriceOffset)
			: base(quotingDirection, quotingVolume)
		{
			_theorPriceOffset = this.Param(nameof(TheorPriceOffset), theorPriceOffset);
		}

		private readonly StrategyParam<Range<Unit>> _theorPriceOffset;

		/// <summary>
		/// Theoretical price offset.
		/// </summary>
		public Range<Unit> TheorPriceOffset
		{
			get => _theorPriceOffset.Value;
			set => _theorPriceOffset.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			var tp = this.GetSecurityValue<decimal?>(Level1Fields.TheorPrice);

			if (tp == null)
				return null;

			if (currentPrice == null || currentPrice < (tp.Value + TheorPriceOffset.Min) || currentPrice > (tp.Value + TheorPriceOffset.Max))
				return tp.Value + (decimal)(TheorPriceOffset.Min + TheorPriceOffset.Length / 2);

			if (currentVolume != newVolume)
				return currentPrice;

			return null;
		}
	}
}