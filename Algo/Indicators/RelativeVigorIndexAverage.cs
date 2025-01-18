namespace StockSharp.Algo.Indicators;

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
		_buffer = new(4);
		Length = _buffer.Capacity;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_buffer.Clear();
		_buffer.Capacity = Length;

		Buffer.Capacity = Length;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
		{
			_buffer.PushBack(candle);
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
						   (candle.ClosePrice - candle.OpenPrice)) / 6m;

				valueDn = ((value1.HighPrice - value1.LowPrice) +
				           2 * (value2.HighPrice - value2.LowPrice) +
				           2 * (value3.HighPrice - value3.LowPrice) +
						   (candle.HighPrice - candle.LowPrice)) / 6m;
			}

			return valueDn == decimal.Zero 
				? valueUp
				: valueUp / valueDn;
		}

		return null;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _buffer.Count >= Length;
}