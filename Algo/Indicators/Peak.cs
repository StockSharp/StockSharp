#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Peak.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Peak.
	/// </summary>
	[DisplayName("Peak")]
	[DescriptionLoc(LocalizedStrings.Str816Key)]
	public sealed class Peak : ZigZagEquis
	{
		/// <summary>
		/// To create the indicator <see cref="Peak"/>.
		/// </summary>
		public Peak()
		{
			ByPrice = c => c.HighPrice;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = base.OnProcess(input);

			if (IsFormed && !value.IsEmpty)
			{
				if (CurrentValue < value.GetValue<decimal>())
				{
					return value;
				}

				var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

				if (input.IsFinal)
					IsFormed = !lastValue.IsEmpty;

				return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
			}

			IsFormed = false;

			return value;
		}
	}
}