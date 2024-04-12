#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Lowest.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Minimum value for a period.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/lowest.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowestKey,
		Description = LocalizedStrings.MinValuePeriodKey)]
	[Doc("topics/api/indicators/list_of_indicators/lowest.html")]
	public class Lowest : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Lowest"/>.
		/// </summary>
		public Lowest()
		{
			Length = 5;
			Buffer.MinComparer = Comparer<decimal>.Default;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, _, low, _) = input.GetOhlc();

			var lastValue = Buffer.Count == 0 ? low : this.GetCurrentValue();

			if (low < lastValue)
				lastValue = low;

			if (input.IsFinal)
			{
				Buffer.AddEx(low);
				lastValue = Buffer.Min.Value;
			}

			return new DecimalIndicatorValue(this, lastValue);
		}
	}
}