namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Индекс товарного канала.
	/// </summary>
	[DisplayName("CCI")]
	[DescriptionLoc(LocalizedStrings.Str760Key)]
	public class CommodityChannelIndex : LengthIndicator<decimal>
	{
		private readonly MeanDeviation _mean = new MeanDeviation();

		/// <summary>
		/// Создать <see cref="CommodityChannelIndex"/>.
		/// </summary>
		public CommodityChannelIndex()
		{
			Length = 15;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_mean.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _mean.IsFormed; } }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			var aveP = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

			var meanValue = _mean.Process(new DecimalIndicatorValue(this, aveP) {IsFinal = input.IsFinal});

			if (IsFormed && meanValue.GetValue<decimal>() != 0)
				return new DecimalIndicatorValue(this, ((aveP - _mean.Sma.GetCurrentValue()) / (0.015m * meanValue.GetValue<decimal>())));

			return new DecimalIndicatorValue(this);
		}
	}
}