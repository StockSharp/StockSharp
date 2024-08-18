namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Median price.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/median_price.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MedPrKey,
		Description = LocalizedStrings.MedianPriceKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/median_price.html")]
	public class MedianPrice : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MedianPrice"/>.
		/// </summary>
		public MedianPrice()
		{
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, low, _) = input.GetOhlc();

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (high + low) / 2);
		}
	}
}
