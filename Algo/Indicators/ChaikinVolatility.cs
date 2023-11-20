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
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Chaikin volatility.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorChaikinVolatility.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChaikinVolatilityKey,
		Description = LocalizedStrings.Str730Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/IndicatorChaikinVolatility.html")]
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

		/// <inheritdoc />
		public override int NumValuesToInitialize => Math.Max(Ema.NumValuesToInitialize, Roc.NumValuesToInitialize);

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <summary>
		/// Moving Average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MAKey,
			Description = LocalizedStrings.MovingAverageKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage Ema { get; }

		/// <summary>
		/// Rate of change.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ROCKey,
			Description = LocalizedStrings.Str732Key,
			GroupName = LocalizedStrings.GeneralKey)]
		public RateOfChange Roc { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => Roc.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var emaValue = Ema.Process(input.SetValue(this, candle.HighPrice - candle.LowPrice));

			if (Ema.IsFormed)
			{
				var val = Roc.Process(emaValue);
				return new DecimalIndicatorValue(this, val.GetValue<decimal>());
			}

			return new DecimalIndicatorValue(this);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Ema.LoadNotNull(storage, nameof(Ema));
			Roc.LoadNotNull(storage, nameof(Roc));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Ema), Ema.Save());
			storage.SetValue(nameof(Roc), Roc.Save());
		}
	}
}
