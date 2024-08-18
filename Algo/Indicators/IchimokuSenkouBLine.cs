namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	/// <summary>
	/// Senkou (B) line.
	/// </summary>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class IchimokuSenkouBLine : LengthIndicator<decimal>
	{
		private readonly CircularBuffer<(decimal, decimal)> _buffer;

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
			var (_, high, low, _) = input.GetOhlc();

			decimal? result = null;
			IList<(decimal high, decimal low)> buff = _buffer;

			if (input.IsFinal)
				_buffer.PushBack((high, low));
			else
				buff = _buffer.Skip(1).Append((high, low)).ToList();

			if (buff.Count >= Length)
			{
				// рассчитываем значение
				var max = buff.Max(t => t.high);
				var min = buff.Min(t => t.low);

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
