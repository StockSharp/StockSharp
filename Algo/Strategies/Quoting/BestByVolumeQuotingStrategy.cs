namespace StockSharp.Algo.Strategies.Quoting
{
	using System;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The quoting according to the Best By Volume rule. For this quoting the volume delta <see cref="VolumeExchange"/> is specified, which can stand in front of the quoted order.
	/// </summary>
	public class BestByVolumeQuotingStrategy : QuotingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BestByVolumeQuotingStrategy"/>.
		/// </summary>
		public BestByVolumeQuotingStrategy()
			: this(Sides.Buy, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BestByVolumeQuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		public BestByVolumeQuotingStrategy(Sides quotingDirection, decimal quotingVolume)
			: base(quotingDirection, quotingVolume)
		{
			_volumeExchange = this.Param(nameof(VolumeExchange), new Unit());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BestByVolumeQuotingStrategy"/>.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		/// <param name="volumeExchange">The volume delta that can stand in front of the quoted order.</param>
		/// <returns>Strategy.</returns>
		public BestByVolumeQuotingStrategy(Order order, Unit volumeExchange)
			: base(order)
		{
			_volumeExchange = this.Param(nameof(VolumeExchange), volumeExchange);
		}

		private readonly StrategyParam<Unit> _volumeExchange;

		/// <summary>
		/// The volume delta that can stand in front of the quoted order.
		/// </summary>
		public Unit VolumeExchange
		{
			get => _volumeExchange.Value;
			set => _volumeExchange.Value = value;
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			var quotes = GetFilteredQuotes(QuotingDirection);

			if (quotes == null || quotes.IsEmpty() && currentPrice == null)
			{
				this.AddWarningLog(LocalizedStrings.NoOrderBookInfo);
				return null;
			}

			var sign = QuotingDirection == Sides.Buy ? 1 : -1;
			var volume = 0m;

			foreach (var quote in quotes)
			{
				if (quote.Price * sign > currentPrice * sign)
				{
					volume += quote.Volume;

					if (volume > VolumeExchange)
					{
						return quote.Price;
					}
				}
				else
					break;
			}

			if (currentPrice != null && currentVolume != newVolume)
				return currentPrice;

			return null;
		}
	}
}