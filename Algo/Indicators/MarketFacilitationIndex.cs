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

	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Market Facilitation Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorMarketFacilitationIndex.html
	/// </remarks>
	[DisplayName("MFI")]
	[DescriptionLoc(LocalizedStrings.Str853Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/IndicatorMarketFacilitationIndex.html")]
	public class MarketFacilitationIndex : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketFacilitationIndex"/>.
		/// </summary>
		public MarketFacilitationIndex()
		{
		}

		/// <inheritdoc />
		public override int? NumValuesToInitialize => 1;

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MunisOnePlusOne;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (candle.TotalVolume == 0)
				return new DecimalIndicatorValue(this);

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (candle.HighPrice - candle.LowPrice) / candle.TotalVolume);
		}
	}
}
