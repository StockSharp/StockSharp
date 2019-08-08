#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: SimpleMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Simple moving average.
	/// </summary>
	[DisplayName("SMA")]
	[DescriptionLoc(LocalizedStrings.Str818Key)]
	public class SimpleMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleMovingAverage"/>.
		/// </summary>
		public SimpleMovingAverage()
		{
			Length = 32;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			if (input.IsFinal)
				return new DecimalIndicatorValue(this, Buffer.Sum() / Length);

			return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
		}
	}
}