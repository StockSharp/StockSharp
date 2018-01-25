#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: VolumeIndicator.cs
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
	/// Candle volume.
	/// </summary>
	[DisplayName("Volume")]
	[DescriptionLoc(LocalizedStrings.Str756Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(CandleIndicatorValue))]
	public class VolumeIndicator : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeIndicator"/>.
		/// </summary>
		public VolumeIndicator()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return new CandleIndicatorValue(this, input.GetValue<Candle>(), c => c.TotalVolume);
		}
	}
}