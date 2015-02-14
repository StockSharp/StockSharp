namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Волатильность Чайкина.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Volatility.ashx
	/// http://www.incrediblecharts.com/indicators/chaikin_volatility.php
	/// </remarks>
	[DisplayName("Chaikin's Volatility")]
	[DescriptionLoc(LocalizedStrings.Str730Key)]
	public class ChaikinVolatility : BaseIndicator<IIndicatorValue>
	{
		/// <summary>
		/// Создать <see cref="ChaikinVolatility"/>.
		/// </summary>
		public ChaikinVolatility()
		{
			Ema = new ExponentialMovingAverage();
			Roc = new RateOfChange();
		}

		/// <summary>
		/// Скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage Ema { get; private set; }

		/// <summary>
		/// Скорость изменения.
		/// </summary>
		[ExpandableObject]
		[DisplayName("ROC")]
		[DescriptionLoc(LocalizedStrings.Str732Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RateOfChange Roc { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Roc.IsFormed; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			var emaValue = Ema.Process(input.SetValue(this, candle.HighPrice - candle.LowPrice));

			if (Ema.IsFormed)
			{
				return Roc.Process(emaValue);
			}

			return input;				
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Ema.LoadNotNull(settings, "Ema");
			Roc.LoadNotNull(settings, "Roc");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("Ema", Ema.Save());
			settings.SetValue("Roc", Roc.Save());
		}
	}
}