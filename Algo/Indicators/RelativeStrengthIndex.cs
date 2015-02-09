namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Индекс относительной силы.
	/// </summary>
	[DisplayName("RSI")]
	[DescriptionLoc(LocalizedStrings.Str770Key)]
	public class RelativeStrengthIndex : LengthIndicator<decimal>
	{
		private readonly SmoothedMovingAverage _gain;
		private readonly SmoothedMovingAverage _loss;
		private bool _isInitialized;
		private decimal _last;

		/// <summary>
		/// Создать <see cref="RelativeStrengthIndex"/>.
		/// </summary>
		public RelativeStrengthIndex()
			: base(typeof(decimal))
		{
			_gain = new SmoothedMovingAverage();
			_loss = new SmoothedMovingAverage();

			Length = 15;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _gain.IsFormed; } }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_loss.Length = _gain.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (!_isInitialized)
			{
				if (input.IsFinal)
				{
					_last = newValue;
					_isInitialized = true;
				}

				return new DecimalIndicatorValue(this);
			}

			var delta = newValue - _last;

			var gainValue = _gain.Process(input.SetValue(this, delta > 0 ? delta : 0m)).GetValue<decimal>();
			var lossValue = _loss.Process(input.SetValue(this, delta > 0 ? 0m : -delta)).GetValue<decimal>();

			if(input.IsFinal)
				_last = newValue;

			if (lossValue == 0)
				return new DecimalIndicatorValue(this, 100m);
			
			if (gainValue / lossValue == 1)
				return new DecimalIndicatorValue(this, 0m);

			return new DecimalIndicatorValue(this, 100m - 100m / (1m + gainValue / lossValue));
		}
	}
}