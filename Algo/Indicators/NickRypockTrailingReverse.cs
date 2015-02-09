namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// NickRypockTrailingReverse (Nick Rypock Trailing reverse).
	/// </summary>
	[DisplayName("NRTR")]
	[Description("Nick Rypock Trailing reverse.")]
	public class NickRypockTrailingReverse : LengthIndicator<decimal>
	{
		private bool _isInitialized;

		private decimal _k;
		private decimal _reverse;
		private decimal _price;
		private decimal _highPrice;
		private decimal _lowPrice;
		private int _newTrend;

		/// <summary>
		/// Направление тренда.
		/// </summary>
		private int _trend;

		private decimal _multiple;

		/// <summary>
		/// Множитель.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str806Key)]
		[DescriptionLoc(LocalizedStrings.Str807Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Мultiple
		{
			get { return _multiple * 1000; }
			set
			{
				decimal tmpValue = value;

				if (tmpValue <= 1)
					tmpValue = 1;

				_multiple = tmpValue / 1000;

				Reset();
			}
		}

		//private int _roundDigits;

		///// <summary>
		///// Округление до знака после запятой.
		///// </summary>
		//[DisplayName("Округление после запятой")]
		//[Description("Округление до знака после запятой.")]
		//[Category("Основное")]
		//public int RoundDigits
		//{
		//	get { return _roundDigits; }
		//	set
		//	{
		//		_roundDigits = value;

		//		if (_roundDigits < 0)
		//			_roundDigits = 0;

		//		Reset();
		//	}
		//}

		/// <summary>
		/// Создать <see cref="NickRypockTrailingReverse"/>.
		/// </summary>
		public NickRypockTrailingReverse()
			: base(typeof(decimal))
		{
			Мultiple = 100;
			Length = 50;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (_isInitialized == false)
			{
				_k = input.GetValue<decimal>();
				_highPrice = input.GetValue<decimal>();
				_lowPrice = input.GetValue<decimal>();

				_isInitialized = true;
			}

			_price = input.GetValue<decimal>();

			_k = (_k + (_price - _k) / Length) * _multiple;

			_newTrend = 0;

			if (_trend >= 0)
			{
				if (_price > _highPrice)
					_highPrice = _price;

				_reverse = _highPrice - _k;

				if (_price <= _reverse)
				{
					_newTrend = -1;
					_lowPrice = _price;
					_reverse = _lowPrice + _k;
				}
				else
				{
					_newTrend = +1;
				}
			}

			if (_trend <= 0)
			{
				if (_price < _lowPrice)
					_lowPrice = _price;

				_reverse = _lowPrice + _k;

				if (_price >= _reverse)
				{
					_newTrend = +1;
					_highPrice = _price;
					_reverse = _highPrice - _k;
				}
				else
				{
					_newTrend = -1;
				}
			}

			if (_newTrend != 0)
				_trend = _newTrend;

			var newValue = _reverse;

			// если буффер стал достаточно большим (стал больше длины)
			if (IsFormed)
			{
				// удаляем хвостовое значение
				Buffer.RemoveAt(0);
			}

			Buffer.Add(newValue);

			// значение NickRypockTrailingReverse
			return new DecimalIndicatorValue(this, newValue);
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_isInitialized = false;

			_k = 0;
			_reverse = 0;
			_price = 0;
			_highPrice = 0;
			_lowPrice = 0;
			_trend = 0;
			_newTrend = 0;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Мultiple = settings.GetValue<decimal>("Multiple");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("Multiple", Мultiple);
		}
	}
}