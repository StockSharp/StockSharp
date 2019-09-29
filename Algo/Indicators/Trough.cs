#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Trough.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Trough.
	/// </summary>
	[DisplayName("Trough")]
	[DescriptionLoc(LocalizedStrings.Str821Key)]
	public sealed class Trough : ZigZagEquis
	{
		/// <summary>
		/// To create the indicator <see cref="Trough"/>.
		/// </summary>
		public Trough()
		{
			ByPrice = c => c.LowPrice;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = base.OnProcess(input);

			if (IsFormed && !value.IsEmpty)
			{
				if (CurrentValue > value.GetValue<decimal>())
				{
					return value;
				}
				else
				{
					var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

					if (input.IsFinal)
						IsFormed = !lastValue.IsEmpty;

					return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
				}
			}

			IsFormed = false;

			return value;
		}
	}
}