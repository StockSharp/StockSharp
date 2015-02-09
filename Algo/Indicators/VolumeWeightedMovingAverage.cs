namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Взвешенная по объему скользящая средняя.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/VMA.ashx
	/// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:vwap_intraday      
	/// </remarks>
	[DisplayName("VMA")]
	[DescriptionLoc(LocalizedStrings.Str823Key)]
	public class VolumeWeightedMovingAverage : LengthIndicator<decimal>
	{
		// Текущее значение числителя
		private readonly Sum _nominator = new Sum();

		// Текущее значение знаменателя
		private readonly Sum _denominator = new Sum();

		/// <summary>
		/// Создать индикатор <see cref="VolumeWeightedMovingAverage"/>.
		/// </summary>
		public VolumeWeightedMovingAverage()
			: base(typeof(Candle))
		{
			Length = 32;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_denominator.Length = _nominator.Length = Length;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _nominator.IsFormed && _denominator.IsFormed; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			var shValue = _nominator.Process(input.SetValue(this, candle.ClosePrice * candle.TotalVolume)).GetValue<decimal>();
			var znValue = _denominator.Process(input.SetValue(this, candle.TotalVolume)).GetValue<decimal>();

			return znValue != 0 
				? new DecimalIndicatorValue(this, (shValue / znValue)) 
				: new DecimalIndicatorValue(this);
		}
	}
}