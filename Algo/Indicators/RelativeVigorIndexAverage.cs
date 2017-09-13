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
	using System.Collections.Generic;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// The weight-average part of indicator <see cref="RelativeVigorIndex"/>.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class RelativeVigorIndexAverage : LengthIndicator<decimal>
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndexAverage"/>.
		/// </summary>
		public RelativeVigorIndexAverage()
		{
			Length = 4;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_buffer.Clear();
			Buffer.Clear();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<Candle>();

			if (input.IsFinal)
			{
				_buffer.Add(newValue);

				if (_buffer.Count > Length)
					_buffer.RemoveAt(0);
			}

			if (IsFormed)
			{
				decimal valueUp, valueDn;

				if (input.IsFinal)
				{
					valueUp = ((_buffer[0].ClosePrice - _buffer[0].OpenPrice) +
					           2*(_buffer[1].ClosePrice - _buffer[1].OpenPrice) +
					           2*(_buffer[2].ClosePrice - _buffer[2].OpenPrice) +
					           (_buffer[3].ClosePrice - _buffer[3].OpenPrice))/6m;

					valueDn = ((_buffer[0].HighPrice - _buffer[0].LowPrice) +
					           2*(_buffer[1].HighPrice - _buffer[1].LowPrice) +
					           2*(_buffer[2].HighPrice - _buffer[2].LowPrice) +
					           (_buffer[3].HighPrice - _buffer[3].LowPrice))/6m;
				}
				else
				{
					valueUp = ((_buffer[1].ClosePrice - _buffer[1].OpenPrice) +
					           2*(_buffer[2].ClosePrice - _buffer[2].OpenPrice) +
					           2*(_buffer[3].ClosePrice - _buffer[3].OpenPrice) +
							   (newValue.ClosePrice - newValue.OpenPrice)) / 6m;

					valueDn = ((_buffer[1].HighPrice - _buffer[1].LowPrice) +
					           2*(_buffer[2].HighPrice - _buffer[2].LowPrice) +
					           2*(_buffer[3].HighPrice - _buffer[3].LowPrice) +
							   (newValue.HighPrice - newValue.LowPrice)) / 6m;
				}

				return new DecimalIndicatorValue(this, valueDn == decimal.Zero 
					? valueUp 
					: valueUp / valueDn);
			}

			return new DecimalIndicatorValue(this);
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _buffer.Count >= Length;
	}
}