#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Highest.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Maximum value for a period.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/highest.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestKey,
		Description = LocalizedStrings.MaxValueForPeriodKey)]
	[Doc("topics/api/indicators/list_of_indicators/highest.html")]
	public class Highest : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Highest"/>.
		/// </summary>
		public Highest()
		{
			Length = 5;
			Buffer.MaxComparer = Comparer<decimal>.Default;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, _, _) = input.GetOhlc();

			var lastValue = Buffer.Count == 0 ? high : this.GetCurrentValue();

			if (high > lastValue)
				lastValue = high;

			if (input.IsFinal)
			{
				Buffer.AddEx(high);
				lastValue = Buffer.Max.Value;
			}

			return new DecimalIndicatorValue(this, lastValue);
		}
	}
}