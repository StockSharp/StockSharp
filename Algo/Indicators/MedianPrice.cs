namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Median price.
	/// </summary>
	[DisplayName("MedianPrice")]
	[DescriptionLoc(LocalizedStrings.Str745Key)]
	public class MedianPrice : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MedianPrice"/>.
		/// </summary>
		public MedianPrice()
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

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (candle.HighPrice + candle.LowPrice) / 2);
		}
	}
}