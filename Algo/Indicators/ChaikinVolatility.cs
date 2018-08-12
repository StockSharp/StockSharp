#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ChaikinVolatility.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Chaikin volatility.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Volatility.ashx http://www.incrediblecharts.com/indicators/chaikin_volatility.php.
	/// </remarks>
	[DisplayName("Chaikin's Volatility")]
	[DescriptionLoc(LocalizedStrings.Str730Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
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
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage Ema { get; }

		/// <summary>
		/// Rate of change.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("ROC")]
		[DescriptionLoc(LocalizedStrings.Str732Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RateOfChange Roc { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Roc.IsFormed;

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
				var val = Roc.Process(emaValue);
				return new DecimalIndicatorValue(this, val.GetValue<decimal>());
			}

			return new DecimalIndicatorValue(this);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Ema.LoadNotNull(settings, nameof(Ema));
			Roc.LoadNotNull(settings, nameof(Roc));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(Ema), Ema.Save());
			settings.SetValue(nameof(Roc), Roc.Save());
		}
	}
}