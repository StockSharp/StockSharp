#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RateOfChange.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Rate of change.
	/// </summary>
	[DisplayName("ROC")]
	[DescriptionLoc(LocalizedStrings.Str732Key)]
	public class RateOfChange : Momentum
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RateOfChange"/>.
		/// </summary>
		public RateOfChange()
		{
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = base.OnProcess(input);

			if (Buffer.Count > 0 && Buffer[0] != 0)
				return new DecimalIndicatorValue(this, result.GetValue<decimal>() / Buffer[0] * 100);
			
			return new DecimalIndicatorValue(this);
		}
	}
}