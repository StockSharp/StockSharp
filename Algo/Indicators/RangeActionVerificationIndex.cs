#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RangeActionVerificationIndex.cs
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

	using StockSharp.Localization;

	/// <summary>
	/// Range Action Verification Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/ravi.html
	/// </remarks>
	[DisplayName("RAVI")]
	[Description("Range Action Verification Index.")]
	[Doc("topics/api/indicators/list_of_indicators/ravi.html")]
	public class RangeActionVerificationIndex : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeActionVerificationIndex"/>.
		/// </summary>
		public RangeActionVerificationIndex()
		{
			ShortSma = new SimpleMovingAverage { Length = 7 };
			LongSma = new SimpleMovingAverage { Length = 65 };
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize => Math.Max(LongSma.NumValuesToInitialize, ShortSma.NumValuesToInitialize);

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// Short moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ShortMaKey,
			Description = LocalizedStrings.ShortMaDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage ShortSma { get; }

		/// <summary>
		/// Long moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LongMaKey,
			Description = LocalizedStrings.LongMaDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage LongSma { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => LongSma.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var shortValue = ShortSma.Process(input).GetValue<decimal>();
			var longValue = LongSma.Process(input).GetValue<decimal>();

			return new DecimalIndicatorValue(this, Math.Abs(100m * (shortValue - longValue) / longValue));
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			ShortSma.LoadIfNotNull(storage, nameof(ShortSma));
			LongSma.LoadIfNotNull(storage, nameof(LongSma));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(ShortSma), ShortSma.Save());
			storage.SetValue(nameof(LongSma), LongSma.Save());
		}
	}
}
