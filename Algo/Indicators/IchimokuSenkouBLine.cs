namespace StockSharp.Algo.Indicators;

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

	/// <inheritdoc />
	public override int NumValuesToInitialize => Kijun.NumValuesToInitialize + base.NumValuesToInitialize - 1;

	/// <summary>
	/// Kijun line.
	/// </summary>
	[Browsable(false)]
	public IchimokuLine Kijun { get; }

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		decimal? result = null;
		IEnumerable<(decimal high, decimal low)> buff;

		if (input.IsFinal)
		{
			_buffer.PushBack((candle.HighPrice, candle.LowPrice));
			buff = _buffer;
		}
		else
			buff = _buffer.Skip(1).Append((candle.HighPrice, candle.LowPrice));

		if (_buffer.Count >= Length)
		{
			var max = buff.Max(t => t.high);
			var min = buff.Min(t => t.low);

			if (Kijun.IsFormed && input.IsFinal)
			   Buffer.PushBack((max + min) / 2);

			if (Buffer.Count >= Kijun.Length)
				result = Buffer[0];

			if (Buffer.Count > Kijun.Length)
				Buffer.PopFront();
		}

		return result;
	}
}
