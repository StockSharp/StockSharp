namespace StockSharp.Algo.Indicators;

/// <summary>
/// The implementation of the lines of Ishimoku KInko Khayo indicator (Tenkan, Kijun, Senkou Span B).
/// </summary>
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorHidden]
public class IchimokuLine : LengthIndicator<decimal>
{
	private readonly CircularBuffer<(decimal, decimal)> _buffer;

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
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		IEnumerable<(decimal high, decimal low)> buff;

		if (input.IsFinal)
		{
			_buffer.PushBack((candle.HighPrice, candle.LowPrice));
			buff = _buffer;
		}
		else
			buff = _buffer.Skip(1).Append((candle.HighPrice, candle.LowPrice));

		if (IsFormed)
		{
			var max = buff.Max(t => t.high);
			var min = buff.Min(t => t.low);

			return (max + min) / 2;
		}

		return null;
	}
}