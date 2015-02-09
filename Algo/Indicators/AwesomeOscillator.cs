namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Чудесный осцилятор.
	/// </summary>
	///  <remarks>
	/// http://ta.mql4.com/indicators/bills/awesome
	/// </remarks>
	[DisplayName("AO")]
	[DescriptionLoc(LocalizedStrings.Str836Key)]
	public class AwesomeOscillator : BaseIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="AwesomeOscillator"/>.
		/// </summary>
		public AwesomeOscillator()
			: this(new SimpleMovingAverage { Length = 34 }, new SimpleMovingAverage { Length = 5 })
		{
		}

		/// <summary>
		/// Создать <see cref="AwesomeOscillator"/>.
		/// </summary>
		/// <param name="longSma">Длинная скользящая средняя.</param>
		/// <param name="shortSma">Короткая скользящая средняя.</param>
		public AwesomeOscillator(SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
		{
			if (longSma == null)
				throw new ArgumentNullException("longSma");

			if (shortSma == null)
				throw new ArgumentNullException("shortSma");

			ShortMa = shortSma;
			LongMa = longSma;
			MedianPrice = new MedianPrice();
		}

		/// <summary>
		/// Длинная скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage LongMa { get; private set; }

		/// <summary>
		/// Короткая скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage ShortMa { get; private set; }

		/// <summary>
		/// Медианная цена.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str843Key)]
		[DescriptionLoc(LocalizedStrings.Str745Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public MedianPrice MedianPrice { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return LongMa.IsFormed; } }

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return MedianPrice.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var mpValue = MedianPrice.Process(input);

			var sValue = ShortMa.Process(mpValue).GetValue<decimal>();
			var lValue = LongMa.Process(mpValue).GetValue<decimal>();

			return new DecimalIndicatorValue(this, sValue - lValue);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			LongMa.LoadNotNull(settings, "LongMa");
			ShortMa.LoadNotNull(settings, "ShortMa");
			MedianPrice.LoadNotNull(settings, "MedianPrice");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("LongMa", LongMa.Save());
			settings.SetValue("ShortMa", ShortMa.Save());
			settings.SetValue("MedianPrice", MedianPrice.Save());
		}
	}
}