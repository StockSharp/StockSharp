#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RelativeVigorIndexAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using Ecng.Collections;

	/// <summary>
	/// The weight-average part of indicator <see cref="RelativeVigorIndex"/>.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorHidden]
	public class RelativeVigorIndexAverage : LengthIndicator<decimal>
	{
		private readonly CircularBuffer<(decimal open, decimal high, decimal low, decimal close)> _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndexAverage"/>.
		/// </summary>
		public RelativeVigorIndexAverage()
		{
			_buffer = new(Length);
			Length = 4;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_buffer.Clear();
			_buffer.Capacity = Length;

			Buffer.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var t = input.GetOhlc();

			if (input.IsFinal)
			{
				_buffer.PushBack(t);
			}

			if (IsFormed)
			{
				decimal valueUp, valueDn;

				var value0 = _buffer[0];
				var value1 = _buffer[1];
				var value2 = _buffer[2];
				var value3 = _buffer[3];

				if (input.IsFinal)
				{
					valueUp = ((value0.close - value0.open) +
					           2 * (value1.close - value1.open) +
					           2 * (value2.close - value2.open) +
					           (value3.close - value3.open)) / 6m;

					valueDn = ((value0.high - value0.low) +
					           2 * (value1.high - value1.low) +
					           2 * (value2.high - value2.low) +
					           (value3.high - value3.low)) / 6m;
				}
				else
				{
					valueUp = ((value1.close - value1.open) +
					           2 * (value2.close - value2.open) +
					           2 * (value3.close - value3.open) +
							   (t.c - t.o)) / 6m;

					valueDn = ((value1.high - value1.low) +
					           2 * (value2.high - value2.low) +
					           2 * (value3.high - value3.low) +
							   (t.h - t.l)) / 6m;
				}

				return new DecimalIndicatorValue(this, valueDn == decimal.Zero 
					? valueUp 
					: valueUp / valueDn);
			}

			return new DecimalIndicatorValue(this);
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _buffer.Count >= Length;
	}
}