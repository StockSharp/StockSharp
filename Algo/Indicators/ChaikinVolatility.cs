namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Chaikin volatility.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Volatility.ashx http://www.incrediblecharts.com/indicators/chaikin_volatility.php.
	/// </remarks>
	[DisplayName("Chaikin's Volatility")]
	[DescriptionLoc(LocalizedStrings.Str730Key)]
	public class ChaikinVolatility : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChaikinVolatility"/>.
		/// </summary>
		public ChaikinVolatility()
		{
			Ema = new ExponentialMovingAverage();
			Roc = new RateOfChange();
		}

		/// <summary>
		/// Moving Average.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage Ema { get; private set; }

		/// <summary>
		/// Rate of change.
		/// </summary>
		[ExpandableObject]
		[DisplayName("ROC")]
		[DescriptionLoc(LocalizedStrings.Str732Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RateOfChange Roc { get; private set; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed
		{
			get { return Roc.IsFormed; }
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
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
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Ema.LoadNotNull(settings, "Ema");
			Roc.LoadNotNull(settings, "Roc");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("Ema", Ema.Save());
			settings.SetValue("Roc", Roc.Save());
		}
	}
}