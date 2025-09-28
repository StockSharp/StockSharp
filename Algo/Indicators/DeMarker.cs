namespace StockSharp.Algo.Indicators;

/// <summary>
/// DeMarker.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/demarker.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DeMarkerKey,
	Description = LocalizedStrings.DeMarkerDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/demarker.html")]
public class DeMarker : LengthIndicator<decimal>
{
	private readonly SimpleMovingAverage _deMaxSma;
	private readonly SimpleMovingAverage _deMinSma;
	private readonly CircularBuffer<decimal> _deMaxBuffer;
	private readonly CircularBuffer<decimal> _deMinBuffer;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="DeMarker"/>.
	/// </summary>
	public DeMarker()
	{
		_deMaxSma = new();
		_deMinSma = new();
		_deMaxBuffer = new(14);
		_deMinBuffer = new(14);
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _deMaxSma.IsFormed && _deMinSma.IsFormed && _isInitialized;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	public override void Reset()
	{
		_deMaxSma.Length = Length;
		_deMinSma.Length = Length;
		_deMaxBuffer.Capacity = Length;
		_deMinBuffer.Capacity = Length;
		_prevHigh = 0;
		_prevLow = 0;
		_isInitialized = false;
		
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var currentHigh = candle.HighPrice;
		var currentLow = candle.LowPrice;

		if (!_isInitialized)
		{
			if (input.IsFinal)
			{
				_prevHigh = currentHigh;
				_prevLow = currentLow;
				_isInitialized = true;
			}

			return new DecimalIndicatorValue(this, input.Time);
		}

		// Calculate DeMax
		var deMax = currentHigh > _prevHigh ? currentHigh - _prevHigh : 0m;
		
		// Calculate DeMin
		var deMin = currentLow < _prevLow ? _prevLow - currentLow : 0m;

		var deMaxSmaValue = _deMaxSma.Process(new DecimalIndicatorValue(_deMaxSma, deMax, input.Time) { IsFinal = input.IsFinal }).ToDecimal();
		var deMinSmaValue = _deMinSma.Process(new DecimalIndicatorValue(_deMinSma, deMin, input.Time) { IsFinal = input.IsFinal }).ToDecimal();

		if (input.IsFinal)
		{
			_deMaxBuffer.PushBack(deMax);
			_deMinBuffer.PushBack(deMin);
			_prevHigh = currentHigh;
			_prevLow = currentLow;
		}

		if (!_deMaxSma.IsFormed || !_deMinSma.IsFormed)
			return new DecimalIndicatorValue(this, input.Time);

		// Calculate DeMarker value
		var denominator = deMaxSmaValue + deMinSmaValue;
		var deMarkerValue = denominator != 0 ? deMaxSmaValue / denominator : 0.5m;

		return new DecimalIndicatorValue(this, deMarkerValue, input.Time);
	}
}