namespace StockSharp.Algo.Indicators;

/// <summary>
/// Choppiness Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CHOPKey,
	Description = LocalizedStrings.ChoppinessIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/choppiness_index.html")]
public class ChoppinessIndex : LengthIndicator<decimal>
{
	private readonly CircularBuffer<decimal> _highLowRange;
	private readonly CircularBuffer<decimal> _trueRange;
	private decimal _prevClose;
	private decimal _sumTrueRange;
	private decimal _sumHighLowRange;
	private decimal _part;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChoppinessIndex"/>.
	/// </summary>
	public ChoppinessIndex()
	{
		const int len = 14;

		_highLowRange = new(len);
		_trueRange = new(len);

		Length = len;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, low, close) = input.GetOhlc();

		var highLowRange = high - low;
		var trueRange = Math.Max(highLowRange, Math.Max(Math.Abs(high - _prevClose), Math.Abs(low - _prevClose)));

		decimal sumTrueRange;
		decimal sumHighLowRange;

		if (input.IsFinal)
		{
			if (IsFormed)
			{
				_sumTrueRange -= _trueRange.Front();
				_sumHighLowRange -= _highLowRange.Front();
			}

			_highLowRange.PushBack(highLowRange);
			_trueRange.PushBack(trueRange);

			_sumTrueRange += trueRange;
			_sumHighLowRange += highLowRange;

			_prevClose = close;

			sumTrueRange = _sumTrueRange;
			sumHighLowRange = _sumHighLowRange;

			IsFormed = _highLowRange.Count == Length;
		}
		else
		{
			sumTrueRange = _sumTrueRange - _trueRange.Front() + trueRange;
			sumHighLowRange = _sumHighLowRange - _highLowRange.Front() + highLowRange;
		}

		if (IsFormed && sumTrueRange != 0)
		{
			var ci = 100 * (decimal)Math.Log10((double)(sumTrueRange / sumHighLowRange)) / _part;
			return new DecimalIndicatorValue(this, ci, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_highLowRange.Clear();
		_trueRange.Clear();

		_highLowRange.Capacity = Length;
		_trueRange.Capacity = Length;

		_sumTrueRange = default;
		_sumHighLowRange = default;

		_prevClose = default;
		_part = (decimal)Math.Log10(Length);

		base.Reset();
	}
}
