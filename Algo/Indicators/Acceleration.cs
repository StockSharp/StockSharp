namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Индикатор Замедления / Ускорения.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/acceleration_deceleration
	/// </remarks>
	[DisplayName("A/D")]
	[DescriptionLoc(LocalizedStrings.Str835Key)]
	public class Acceleration : BaseIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="Acceleration"/>.
		/// </summary>
		public Acceleration()
			: this(new AwesomeOscillator(), new SimpleMovingAverage { Length = 5 })
		{
		}

		/// <summary>
		/// Создать <see cref="Acceleration"/>.
		/// </summary>
		/// <param name="ao">Чудесный осцилятор.</param>
		/// <param name="sma">Cкользящая средняя.</param>
		public Acceleration(AwesomeOscillator ao, SimpleMovingAverage sma)
			: base(typeof(Candle))
		{
			if (ao == null)
				throw new ArgumentNullException("ao");

			if (sma == null)
				throw new ArgumentNullException("sma");

			Ao = ao;
			Sma = sma;
		}

		/// <summary>
		/// Cкользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage Sma { get; private set; }

		/// <summary>
		/// Чудесный осцилятор.
		/// </summary>
		[ExpandableObject]
		[DisplayName("AO")]
		[DescriptionLoc(LocalizedStrings.Str836Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AwesomeOscillator Ao { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return Sma.IsFormed; } }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var aoValue = Ao.Process(input);

			if (Ao.IsFormed)
				return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>() - Sma.Process(aoValue).GetValue<decimal>());

			return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>());
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Sma.LoadNotNull(settings, "Sma");
			Ao.LoadNotNull(settings, "Ao");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("Sma", Sma.Save());
			settings.SetValue("Ao", Ao.Save());
		}
	}
}