namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System;

	using Ecng.Collections;
	using Ecng.Serialization;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптивная скользящая средняя Кауфманна.
	/// </summary>
	[DisplayName("KAMA")]
	[DescriptionLoc(LocalizedStrings.Str792Key)]
	public class KaufmannAdaptiveMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;
		private bool _isInitialized;

		/// <summary>
		/// Создать <see cref="KaufmannAdaptiveMovingAverage"/>.
		/// </summary>
		public KaufmannAdaptiveMovingAverage()
			: base(typeof(decimal))
		{
			FastSCPeriod = 2;
			SlowSCPeriod = 30;
		}

		/// <summary>
		/// Период "быстрой" EMA. Значение по умолчанию 2.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str793Key)]
		[DescriptionLoc(LocalizedStrings.Str794Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int FastSCPeriod { get; set; }

		/// <summary>
		/// Период "медленной" EMA. Значение по умолчанию 30.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str795Key)]
		[DescriptionLoc(LocalizedStrings.Str796Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int SlowSCPeriod { get; set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Buffer.Count > Length; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_prevFinalValue = 0;
			_isInitialized = false;

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
			var lastValue = this.GetCurrentValue();

			if (input.IsFinal)
				Buffer.Add(newValue);

			if (!IsFormed)
				return new DecimalIndicatorValue(this, lastValue);

			if (!_isInitialized && Buffer.Count == Length + 1)
			{
				_isInitialized = true;
				// Начальное значение - последнее входное значение.
				return new DecimalIndicatorValue(this, _prevFinalValue = newValue);
			}

			var buff = Buffer;

			if (input.IsFinal)
			{
				buff.RemoveAt(0);
			}
			else
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			var direction = newValue - buff[0];

			decimal volatility = 0;

			for (int i = 1; i < buff.Count; i++)
			{
				volatility += Math.Abs(buff[i] - buff[i - 1]);
			}

			volatility = volatility > 0 ? volatility : 0.00001m;

			var er = Math.Abs(direction / volatility);

			var fastSC = 2m / (FastSCPeriod + 1m);
			var slowSC = 2m / (SlowSCPeriod + 1m);

			var ssc = er * (fastSC - slowSC) + slowSC;
			var smooth = (ssc * ssc);

			var curValue = (newValue - _prevFinalValue) * smooth + _prevFinalValue;
			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			FastSCPeriod = settings.GetValue<int>("FastSCPeriod");
			FastSCPeriod = settings.GetValue<int>("FastSCPeriod");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("FastSCPeriod", FastSCPeriod);
			settings.SetValue("SlowSCPeriod", SlowSCPeriod);
		}
	}
}