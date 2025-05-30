namespace StockSharp.Algo.Indicators;

/// <summary>
/// The part of the indicator <see cref="DirectionalIndex"/>.
/// </summary>
[IndicatorIn(typeof(CandleIndicatorValue))]
public abstract class DiPart : LengthIndicator<decimal>
{
	private readonly AverageTrueRange _averageTrueRange;
	private readonly WilderMovingAverage _movingAverage;
	private ICandleMessage _lastCandle;

	/// <summary>
	/// Initialize <see cref="DiPart"/>.
	/// </summary>
	protected DiPart()
	{
		_averageTrueRange = new();
		_movingAverage = new();

		Length = 5;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_averageTrueRange.Length = Length;
		_movingAverage.Length = Length;

		_lastCandle = null;
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 2;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		decimal? result = null;

		var candle = input.ToCandle();

		// 1 period delay
		if (input.IsFinal && _averageTrueRange.IsFormed && _movingAverage.IsFormed)
			IsFormed = true;

		var trValue = _averageTrueRange.Process(input);

		if (_lastCandle != null)
		{
			var trValueDec = trValue.ToDecimal();
			var maValue = _movingAverage.Process(new DecimalIndicatorValue(this, GetValue(candle, _lastCandle), input.Time) { IsFinal = input.IsFinal });

			if (!maValue.IsEmpty)
				result = trValueDec != 0m ? 100m * maValue.ToDecimal() / trValueDec : 0m;
		}

		if (input.IsFinal)
			_lastCandle = candle;

		return result;
	}

	/// <summary>
	/// To get the part value.
	/// </summary>
	/// <param name="current">The current candle.</param>
	/// <param name="prev">The previous candle.</param>
	/// <returns>Value.</returns>
	protected abstract decimal GetValue(ICandleMessage current, ICandleMessage prev);
}