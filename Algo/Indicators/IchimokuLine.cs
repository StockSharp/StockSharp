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
	using System.Linq;

	using MoreLinq;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// The implementation of the lines of Ishimoku KInko Khayo indicator (Tenkan, Kijun, Senkou Span B).
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class IchimokuLine : LengthIndicator<decimal>
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuLine"/>.
		/// </summary>
		public IchimokuLine()
		{
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_buffer.Clear();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _buffer.Count >= Length;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			var buff = _buffer;

			if (input.IsFinal)
			{
				_buffer.Add(candle);

				// если буффер стал достаточно большим (стал больше длины)
				if (_buffer.Count > Length)
					_buffer.RemoveAt(0);
			}
			else
				buff = _buffer.Skip(1).Concat(candle).ToList();

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