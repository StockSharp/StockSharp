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
	public class VolumeIndicator : BaseIndicator<IIndicatorValue>
	{
		/// <summary>
		/// Создать <see cref="VolumeIndicator"/>.
		/// </summary>
		public VolumeIndicator()
			: base(typeof(Candle))
		{
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return true; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			return new CandleIndicatorValue(this, input.GetValue<Candle>(), c => c.TotalVolume);
		}
	}
}