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

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Maximum value for a period.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorHighest.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestKey,
		Description = LocalizedStrings.Str733Key)]
	[Doc("topics/IndicatorHighest.html")]
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
			var newValue = input.IsSupport(typeof(ICandleMessage)) ? input.GetValue<ICandleMessage>().HighPrice : input.GetValue<decimal>();
			var lastValue = Buffer.Count == 0 ? newValue : this.GetCurrentValue();

			if (newValue > lastValue)
				lastValue = newValue;

			if (input.IsFinal)
			{
				Buffer.AddEx(newValue);
				lastValue = Buffer.Max.Value;
			}

			return new DecimalIndicatorValue(this, lastValue);
		}
	}
}