namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Схождение/расхождение скользящих средних.
	/// </summary>
	[DisplayName("MACD")]
	[DescriptionLoc(LocalizedStrings.Str797Key)]
	public class MovingAverageConvergenceDivergence : BaseIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergence"/>.
		/// </summary>
		public MovingAverageConvergenceDivergence()
			: this(new ExponentialMovingAverage { Length = 26 }, new ExponentialMovingAverage { Length = 12 })
		{
		}

		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergence"/>.
		/// </summary>
		/// <param name="longMa">Длинная скользящая средняя.</param>
		/// <param name="shortMa">Короткая скользящая средняя.</param>
		public MovingAverageConvergenceDivergence(ExponentialMovingAverage longMa, ExponentialMovingAverage shortMa)
		{
			if (longMa == null)
				throw new ArgumentNullException("longMa");

			if (shortMa == null)
				throw new ArgumentNullException("shortMa");

			ShortMa = shortMa;
			LongMa = longMa;
		}

		/// <summary>
		/// Длинная скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage LongMa { get; private set; }

		/// <summary>
		/// Короткая скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str801Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage ShortMa { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return LongMa.IsFormed; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var shortValue = ShortMa.Process(input);
			var longValue = LongMa.Process(input);
			return new DecimalIndicatorValue(this, shortValue.GetValue<decimal>() - longValue.GetValue<decimal>());
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
		}
	}
}