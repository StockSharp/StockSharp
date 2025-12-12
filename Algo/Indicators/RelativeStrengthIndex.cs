namespace StockSharp.Algo.Indicators;

/// <summary>
/// Relative Strength Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/rsi.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RSIKey,
	Description = LocalizedStrings.RelativeStrengthIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/rsi.html")]
public class RelativeStrengthIndex : DecimalLengthIndicator
{
	private readonly SmoothedMovingAverage _gain;
	private readonly SmoothedMovingAverage _loss;
	private bool _isInitialized;
	private decimal _last;
	private decimal? _prevResult;

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeStrengthIndex"/>.
	/// </summary>
	public RelativeStrengthIndex()
	{
		_gain = new();
		_loss = new();

		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _gain.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_last = default;
		_isInitialized = default;
		_prevResult = default;
		_loss.Length = _gain.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal(Source);

		if (!_isInitialized)
		{
			if (input.IsFinal)
			{
				_last = newValue;
				_isInitialized = true;
			}

			return null;
		}

		var delta = newValue - _last;

		var gainValue = _gain.Process(input, delta > 0 ? delta : 0m).ToDecimal(Source);
		var lossValue = _loss.Process(input, delta > 0 ? 0m : -delta).ToDecimal(Source);

		if(input.IsFinal)
			_last = newValue;

		// Stable RSI computation without risky division by (near) zero:
		// RSI = 100 * avgGain / (avgGain + avgLoss)
		var sum = gainValue + lossValue;

		if (sum == 0m)
			return _prevResult ?? 50m;

		var rsi = 100m * gainValue / sum;

		if (input.IsFinal)
			_prevResult = rsi;

		return rsi;
	}
}