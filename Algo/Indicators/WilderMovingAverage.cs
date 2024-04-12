#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: WilderMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Welles Wilder Moving Average.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/wilder_ma.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WilderMAKey,
		Description = LocalizedStrings.WilderMovingAverageKey)]
	[Doc("topics/api/indicators/list_of_indicators/wilder_ma.html")]
	public class WilderMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WilderMovingAverage"/>.
		/// </summary>
		public WilderMovingAverage()
		{
			Length = 32;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
				Buffer.AddEx(newValue);

			var buffCount = input.IsFinal ? Buffer.Count : ((Buffer.Count - 1).Max(0) + 1);

			return new DecimalIndicatorValue(this, (this.GetCurrentValue() * (buffCount - 1) + newValue) / buffCount);
		}
	}
}