namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Range Action Verification Index.
	/// </summary>
	[DisplayName("RAVI")]
	[Description("Range Action Verification Index.")]
	public class RangeActionVerificationIndex : BaseIndicator
	{
		/// <summary>
		/// Создать <see cref="RangeActionVerificationIndex"/>.
		/// </summary>
		public RangeActionVerificationIndex()
		{
			ShortSma = new SimpleMovingAverage();
			LongSma = new SimpleMovingAverage();
		}

		/// <summary>
		/// Короткая скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str801Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage ShortSma { get; private set; }

		/// <summary>
		/// Длинная скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage LongSma { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return LongSma.IsFormed; } }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var shortValue = ShortSma.Process(input).GetValue<decimal>();
			var longValue = LongSma.Process(input).GetValue<decimal>();

			return new DecimalIndicatorValue(this, Math.Abs(100m * (shortValue - longValue) / longValue));
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			ShortSma.LoadNotNull(settings, "ShortSma");
			LongSma.LoadNotNull(settings, "LongSma");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("ShortSma", ShortSma.Save());
			settings.SetValue("LongSma", LongSma.Save());
		}
	}
}