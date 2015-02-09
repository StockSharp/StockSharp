namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Скользящая средняя Халла (Hull Moving Average).
	/// </summary>
	[DisplayName("HMA")]
	[DescriptionLoc(LocalizedStrings.Str786Key)]
	public class HullMovingAverage : LengthIndicator<decimal>
	{
		private readonly WeightedMovingAverage _wmaSlow = new WeightedMovingAverage();
		private readonly WeightedMovingAverage _wmaFast = new WeightedMovingAverage();
		private readonly WeightedMovingAverage _wmaResult = new WeightedMovingAverage();

		/// <summary>
		/// Создать <see cref="HullMovingAverage"/>.
		/// </summary>
		public HullMovingAverage()
			: base(typeof(decimal))
		{
			Length = 10;
			SqrtPeriod = 0;
		}

		private int _sqrtPeriod;

		/// <summary>
		/// Период результирующей средней. Если равно 0, период результирующей средней равен квадратному корню периода HMA.
		/// По умолчанию равно 0. 
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str787Key)]
		[DescriptionLoc(LocalizedStrings.Str788Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int SqrtPeriod
		{
			get { return _sqrtPeriod; }
			set
			{
				_sqrtPeriod = value;
				_wmaResult.Length = value == 0 ? (int)(Math.Sqrt(Length)) : value;
			}
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _wmaResult.IsFormed; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_wmaSlow.Length = Length;
			_wmaFast.Length = Length / 2;
			_wmaResult.Length = SqrtPeriod == 0 ? (int)(Math.Sqrt(Length)) : SqrtPeriod;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_wmaSlow.Process(input);
			_wmaFast.Process(input);

			if (_wmaFast.IsFormed && _wmaSlow.IsFormed)
			{
				var diff = 2 * _wmaFast.GetCurrentValue() - _wmaSlow.GetCurrentValue();
				_wmaResult.Process(diff);
			}

			return new DecimalIndicatorValue(this, _wmaResult.GetCurrentValue());
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			SqrtPeriod = settings.GetValue<int>("SqrtPeriod");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("SqrtPeriod", SqrtPeriod);
		}
	}
}