#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: MovingAverageConvergenceDivergence.cs
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
	/// Convergence/divergence of moving averages.
	/// </summary>
	[DisplayName("MACD")]
	[DescriptionLoc(LocalizedStrings.Str797Key)]
	public class MovingAverageConvergenceDivergence : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergence"/>.
		/// </summary>
		public MovingAverageConvergenceDivergence()
			: this(new ExponentialMovingAverage { Length = 26 }, new ExponentialMovingAverage { Length = 12 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergence"/>.
		/// </summary>
		/// <param name="longMa">Long moving average.</param>
		/// <param name="shortMa">Short moving average.</param>
		public MovingAverageConvergenceDivergence(ExponentialMovingAverage longMa, ExponentialMovingAverage shortMa)
		{
			ShortMa = shortMa ?? throw new ArgumentNullException(nameof(shortMa));
			LongMa = longMa ?? throw new ArgumentNullException(nameof(longMa));
		}

		/// <summary>
		/// Long moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage LongMa { get; }

		/// <summary>
		/// Short moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str801Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage ShortMa { get; }

		/// <inheritdoc />
		public override bool IsFormed => LongMa.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var shortValue = ShortMa.Process(input);
			var longValue = LongMa.Process(input);
			return new DecimalIndicatorValue(this, shortValue.GetValue<decimal>() - longValue.GetValue<decimal>());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			LongMa.LoadNotNull(storage, nameof(LongMa));
			ShortMa.LoadNotNull(storage, nameof(ShortMa));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(LongMa), LongMa.Save());
			storage.SetValue(nameof(ShortMa), ShortMa.Save());
		}
	}
}