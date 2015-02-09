namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Медианная цена.
	/// </summary>
	[DisplayName("MedianPrice")]
	[DescriptionLoc(LocalizedStrings.Str745Key)]
	public class MedianPrice : BaseIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="MedianPrice"/>.
		/// </summary>
		public MedianPrice()
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
			var candle = input.GetValue<Candle>();
			return new DecimalIndicatorValue(this, (candle.HighPrice + candle.LowPrice) / 2);
		}
	}
}