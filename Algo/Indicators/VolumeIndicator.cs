namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Объем свечи.
	/// </summary>
	[DisplayName("Volume")]
	[DescriptionLoc(LocalizedStrings.Str756Key)]
	public class VolumeIndicator : BaseIndicator
	{
		/// <summary>
		/// Создать <see cref="VolumeIndicator"/>.
		/// </summary>
		public VolumeIndicator()
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return new CandleIndicatorValue(this, input.GetValue<Candle>(), c => c.TotalVolume);
		}
	}
}