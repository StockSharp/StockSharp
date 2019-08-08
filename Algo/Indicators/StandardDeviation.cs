#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: StandardDeviation.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Standard deviation.
	/// </summary>
	[DisplayName("StdDev")]
	[DescriptionLoc(LocalizedStrings.Str820Key)]
	public class StandardDeviation : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardDeviation"/>.
		/// </summary>
		public StandardDeviation()
		{
			_sma = new SimpleMovingAverage();
			Length = 10;
		}

		/// <inheritdoc />
		public override bool IsFormed => _sma.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();
			var smaValue = _sma.Process(input).GetValue<decimal>();

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

			//считаем значение отклонения в последней точке
			var std = buff.Select(t1 => t1 - smaValue).Select(t => t * t).Sum();

			return new DecimalIndicatorValue(this, (decimal)Math.Sqrt((double)(std / Length)));
		}
	}
}