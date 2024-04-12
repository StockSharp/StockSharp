#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: MedianPrice.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Median price.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/median_price.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MedPrKey,
		Description = LocalizedStrings.MedianPriceKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/median_price.html")]
	public class MedianPrice : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MedianPrice"/>.
		/// </summary>
		public MedianPrice()
		{
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, low, _) = input.GetOhlc();

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (high + low) / 2);
		}
	}
}
