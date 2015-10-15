namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Cadle volume.
	/// </summary>
	[DisplayName("Volume")]
	[DescriptionLoc(LocalizedStrings.Str756Key)]
	public class VolumeIndicator : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeIndicator"/>.
		/// </summary>
		public VolumeIndicator()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return new CandleIndicatorValue(this, input.GetValue<Candle>(), c => c.TotalVolume);
		}
	}
}