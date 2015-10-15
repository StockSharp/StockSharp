namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Market Facilitation Index.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/market_facilitation_index.
	/// </remarks>
	[DisplayName("MFI")]
	[DescriptionLoc(LocalizedStrings.Str853Key)]
	public class MarketFacilitationIndex : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketFacilitationIndex"/>.
		/// </summary>
		public MarketFacilitationIndex()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (candle.TotalVolume == 0)
				return new DecimalIndicatorValue(this);

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (candle.HighPrice - candle.LowPrice) / candle.TotalVolume);
		}
	}
}