#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: MarketFacilitationIndex.cs
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
	/// Market Facilitation Index.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/market_facilitation_index.
	/// </remarks>
	[DisplayName("MFI")]
	[DescriptionLoc(LocalizedStrings.Str853Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class MarketFacilitationIndex : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketFacilitationIndex"/>.
		/// </summary>
		public MarketFacilitationIndex()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (candle.TotalVolume == 0)
				return new DecimalIndicatorValue(this);

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (candle.HighPrice - candle.LowPrice) / candle.TotalVolume);
		}
	}
}