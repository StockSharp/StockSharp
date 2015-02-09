namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Осциллятор ценовых моментов Чанде.
	/// </summary>
	[DisplayName("CMO")]
	[DescriptionLoc(LocalizedStrings.Str759Key)]
	public class ChandeMomentumOscillator : LengthIndicator<decimal>
	{
		private readonly Sum _cmoUp = new Sum();
		private readonly Sum _cmoDn = new Sum();
		private bool _isInitialized;
		private decimal _last;

		/// <summary>
		/// Создать <see cref="ChandeMomentumOscillator"/>.
		/// </summary>
		public ChandeMomentumOscillator()
			: base(typeof(decimal))
		{
			Length = 15;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_cmoDn.Length = _cmoUp.Length = Length;
			_isInitialized = false;
			_last = 0;

			base.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _cmoUp.IsFormed; } }

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

			var upValue = _cmoUp.Process(input.SetValue(this, delta > 0 ? delta : 0m)).GetValue<decimal>();
			var downValue = _cmoDn.Process(input.SetValue(this, delta > 0 ? 0m : -delta)).GetValue<decimal>();

			if(input.IsFinal)
				_last = newValue;

			var value = (upValue + downValue) == 0 ? 0 : 100m * (upValue - downValue) / (upValue + downValue);

			return IsFormed ? new DecimalIndicatorValue(this, value) : new DecimalIndicatorValue(this);
		}
	}
}