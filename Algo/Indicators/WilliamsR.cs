#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: WilliamsR.cs
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
	/// Williams Percent Range.
	/// </summary>
	/// <remarks>
	/// %R = (Highest High - Close)/(Highest High - Lowest Low) * -100
	/// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:williams_r
	/// http://www2.wealth-lab.com/WL5Wiki/WilliamsR.ashx.
	/// </remarks>
	[DisplayName("%R")]
	[DescriptionLoc(LocalizedStrings.Str854Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class WilliamsR : LengthIndicator<decimal>
	{
		// Текущее значение минимума
		private readonly Lowest _low;

		// Текущее значение максимума
		private readonly Highest _high;

		/// <summary>
		/// Initializes a new instance of the <see cref="WilliamsR"/>.
		/// </summary>
		public WilliamsR()
		{
			_low = new Lowest();
			_high = new Highest();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _low.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_high.Length = _low.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			// Находим минимум и максимум для заданного периода
			var lowValue = _low.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();
			var highValue = _high.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();

			if ((highValue - lowValue) != 0)
				return new DecimalIndicatorValue(this, -100m * (highValue - candle.ClosePrice) / (highValue - lowValue));
				
			return new DecimalIndicatorValue(this);
		}
	}
}