#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: CommodityChannelIndex.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Commodity Channel Index.
	/// </summary>
	[DisplayName("CCI")]
	[DescriptionLoc(LocalizedStrings.Str760Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class CommodityChannelIndex : LengthIndicator<decimal>
	{
		private readonly MeanDeviation _mean = new MeanDeviation();

		/// <summary>
		/// Initializes a new instance of the <see cref="CommodityChannelIndex"/>.
		/// </summary>
		public CommodityChannelIndex()
		{
			Length = 15;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_mean.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _mean.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			var aveP = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

			var meanValue = _mean.Process(new DecimalIndicatorValue(this, aveP) {IsFinal = input.IsFinal});

			if (IsFormed && meanValue.GetValue<decimal>() != 0)
				return new DecimalIndicatorValue(this, ((aveP - _mean.Sma.GetCurrentValue()) / (0.015m * meanValue.GetValue<decimal>())));

			return new DecimalIndicatorValue(this);
		}
	}
}