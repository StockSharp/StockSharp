#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IchimokuLine.cs
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

	using StockSharp.Algo.Candles;

	/// <summary>
	/// The implementation of the lines of Ishimoku KInko Khayo indicator (Tenkan, Kijun, Senkou Span B).
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Browsable(false)]
	public class IchimokuLine : LengthIndicator<decimal>
	{
		private readonly CircularBuffer<Candle> _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuLine"/>.
		/// </summary>
		public IchimokuLine()
		{
			_buffer = new(Length);
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_buffer.Clear();
			_buffer.Capacity = Length;
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _buffer.Count >= Length;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			IList<Candle> buff = _buffer;

			if (input.IsFinal)
				_buffer.PushBack(candle);
			else
				buff = _buffer.Skip(1).Append(candle).ToList();

			if (IsFormed)
			{
				// рассчитываем значение
				var max = buff.Max(t => t.HighPrice);
				var min = buff.Min(t => t.LowPrice);

				return new DecimalIndicatorValue(this, (max + min) / 2);
			}
				
			return new DecimalIndicatorValue(this);
		}
	}
}