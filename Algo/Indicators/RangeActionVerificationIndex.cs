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

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Range Action Verification Index.
	/// </summary>
	[DisplayName("RAVI")]
	[Description("Range Action Verification Index.")]
	public class RangeActionVerificationIndex : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeActionVerificationIndex"/>.
		/// </summary>
		public RangeActionVerificationIndex()
		{
			ShortSma = new SimpleMovingAverage();
			LongSma = new SimpleMovingAverage();
		}

		/// <summary>
		/// Short moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str801Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage ShortSma { get; }

		/// <summary>
		/// Long moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage LongSma { get; }

		/// <inheritdoc />
		public override bool IsFormed => LongSma.IsFormed;

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

			ShortSma.LoadNotNull(storage, nameof(ShortSma));
			LongSma.LoadNotNull(storage, nameof(LongSma));
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