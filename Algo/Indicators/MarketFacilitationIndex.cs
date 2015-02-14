namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Индекс Облегчения Рынка.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/market_facilitation_index
	/// </remarks>
	[DisplayName("MFI")]
	[DescriptionLoc(LocalizedStrings.Str853Key)]
	public class MarketFacilitationIndex : BaseIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="MarketFacilitationIndex"/>.
		/// </summary>
		public MarketFacilitationIndex()
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			IsFormed = true;
			var candle = input.GetValue<Candle>();

			if (candle.TotalVolume == 0)
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, (candle.HighPrice - candle.LowPrice) / candle.TotalVolume);
		}
	}
}