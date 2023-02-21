#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IchimokuSenkouBLine.cs
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

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Senkou (B) line.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class IchimokuSenkouBLine : LengthIndicator<decimal>
	{
		private readonly CircularBuffer<Candle> _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuLine"/>.
		/// </summary>
		/// <param name="kijun">Kijun line.</param>
		public IchimokuSenkouBLine(IchimokuLine kijun)
		{
			Kijun = kijun ?? throw new ArgumentNullException(nameof(kijun));
			_buffer = new(Length);
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_buffer.Clear();
			_buffer.Capacity = Length;
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _buffer.Count >= Length && Buffer.Count >= Kijun.Length;

		/// <summary>
		/// Kijun line.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Kijun { get; }

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			
			decimal? result = null;
			IList<Candle> buff = _buffer;

			if (input.IsFinal)
				_buffer.PushBack(candle);
			else
				buff = _buffer.Skip(1).Append(candle).ToList();

			if (buff.Count >= Length)
			{
				// рассчитываем значение
				var max = buff.Max(t => t.HighPrice);
				var min = buff.Min(t => t.LowPrice);

				if (Kijun.IsFormed && input.IsFinal)
				   Buffer.PushBack((max + min) / 2);

				if (Buffer.Count >= Kijun.Length)
					result = Buffer[0];

				if (Buffer.Count > Kijun.Length)
					Buffer.PopFront();
			}

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}
	}
}
