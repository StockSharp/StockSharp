#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: WeightedMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	[DisplayName("WMA")]
	[DescriptionLoc(LocalizedStrings.Str824Key)]
	public class WeightedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _denominator = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="WeightedMovingAverage"/>.
		/// </summary>
		public WeightedMovingAverage()
		{
			Length = 32;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_denominator = 0;

			for (var i = 1; i <= Length; i++)
				_denominator += i;
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

			var buff = Buffer;
			if (!input.IsFinal)
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			var w = 1;
			return new DecimalIndicatorValue(this, buff.Sum(v => w++ * v) / _denominator);
		}
	}
}