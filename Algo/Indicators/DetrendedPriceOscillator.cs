namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Бестрендовый ценовой осциллятор.
	/// </summary>
	[DisplayName("DPO")]
	[DescriptionLoc(LocalizedStrings.Str761Key)]
	public class DetrendedPriceOscillator : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma = new SimpleMovingAverage();

		/// <summary>
		/// Создать <see cref="DetrendedPriceOscillator"/>.
		/// </summary>
		public DetrendedPriceOscillator()
		{
			_sma = new SimpleMovingAverage();
			Length = 3;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = (Length - 2) * 2;
			base.Reset();
		}

		/// <summary>
		/// Индикатор сформирован.
		/// </summary>
		public override bool IsFormed { get { return Buffer.Count >= Length; } }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var smaValue = _sma.Process(input);

			if (_sma.IsFormed && input.IsFinal)
				Buffer.Add(smaValue.GetValue<decimal>());

			if (!IsFormed)
				return new DecimalIndicatorValue(this);

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			return new DecimalIndicatorValue(this, input.GetValue<decimal>() - Buffer[0]);
		}
	}
}