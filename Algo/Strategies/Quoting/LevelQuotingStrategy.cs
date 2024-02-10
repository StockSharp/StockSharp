namespace StockSharp.Algo.Strategies.Quoting
{
	using System;
	using System.Linq;

	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The quoting by specified level in the order book.
	/// </summary>
	public class LevelQuotingStrategy : QuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LevelQuotingStrategy"/>.
		/// </summary>
		public LevelQuotingStrategy()
			: this(Sides.Buy, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LevelQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		public LevelQuotingStrategy(Sides quotingDirection, decimal quotingVolume)
			: base(quotingDirection, quotingVolume)
		{
			_level = this.Param(nameof(Level), new Range<int>());
			_ownLevel = this.Param<bool>(nameof(OwnLevel));
		}

		private readonly StrategyParam<Range<int>> _level;

		/// <summary>
		/// The level in the order book. It specifies the number of quotes to the deep from the best one. By default, it is equal to {0:0} which means quoting by the best quote.
		/// </summary>
		public Range<int> Level
		{
			get => _level.Value;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value.Contains(-1))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (value == Level)
					return;

				_level.Value = value;
			}
		}

		private readonly StrategyParam<bool> _ownLevel;

		/// <summary>
		/// To create your own price level in the order book, if there is no quote with necessary price yet. The default is disabled.
		/// </summary>
		public bool OwnLevel
		{
			get => _ownLevel.Value;
			set => _ownLevel.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			var quotes = GetFilteredQuotes(QuotingDirection);

			var f = quotes?.ElementAtOr(Level.Min);

			if (f == null)
				return null;

			var from = f.Value;

			var to = quotes.ElementAtOr(Level.Max);

			decimal toPrice;

			if (to == null)
			{
				toPrice = OwnLevel
					? (decimal)(from.Price + (QuotingDirection == Sides.Sell ? 1 : -1) * Level.Length.Pips(this.GetSecurity()))
					: quotes.Last().Price;
			}
			else
				toPrice = to.Value.Price;

			if (QuotingDirection == Sides.Sell)
			{
				if (from.Price > currentPrice || currentPrice > toPrice)
					return (from.Price + toPrice) / 2;
			}
			else
			{
				if (toPrice > currentPrice && currentPrice > from.Price)
					return (toPrice + from.Price) / 2;
			}

			if (currentPrice != null && currentVolume != newVolume)
				return currentPrice;

			return null;
		}
	}
}