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
	using System.ComponentModel;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The weight-average part of indicator <see cref="RelativeVigorIndex"/>.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorHidden]
	public class RelativeVigorIndexAverage : LengthIndicator<decimal>
	{
		private readonly CircularBuffer<ICandleMessage> _buffer;

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
			var newValue = input.GetValue<ICandleMessage>();

			if (input.IsFinal)
			{
				_buffer.PushBack(newValue);
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
					valueUp = ((value0.ClosePrice - value0.OpenPrice) +
					           2 * (value1.ClosePrice - value1.OpenPrice) +
					           2 * (value2.ClosePrice - value2.OpenPrice) +
					           (value3.ClosePrice - value3.OpenPrice)) / 6m;

					valueDn = ((value0.HighPrice - value0.LowPrice) +
					           2 * (value1.HighPrice - value1.LowPrice) +
					           2 * (value2.HighPrice - value2.LowPrice) +
					           (value3.HighPrice - value3.LowPrice)) / 6m;
				}
				else
				{
					valueUp = ((value1.ClosePrice - value1.OpenPrice) +
					           2 * (value2.ClosePrice - value2.OpenPrice) +
					           2 * (value3.ClosePrice - value3.OpenPrice) +
							   (newValue.ClosePrice - newValue.OpenPrice)) / 6m;

					valueDn = ((value1.HighPrice - value1.LowPrice) +
					           2 * (value2.HighPrice - value2.LowPrice) +
					           2 * (value3.HighPrice - value3.LowPrice) +
							   (newValue.HighPrice - newValue.LowPrice)) / 6m;
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