namespace StockSharp.Algo.Indicators;

/// <summary>
/// Pretty Good Oscillator (PGO).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PGOKey,
	Description = LocalizedStrings.PrettyGoodOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/pretty_good_oscillator.html")]
public class PrettyGoodOscillator : LengthIndicator<decimal>
{
	private readonly SimpleMovingAverage _sma = new();
	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="PrettyGoodOscillator"/>.
	/// </summary>
	public PrettyGoodOscillator()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _sma.IsFormed && _highest.IsFormed && _lowest.IsFormed;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var smaValue = _sma.Process(input, candle.ClosePrice);
		var highestValue = _highest.Process(input, candle.HighPrice);
		var lowestValue = _lowest.Process(input, candle.LowPrice);

		if (IsFormed)
		{
			var sma = smaValue.ToDecimal();
			var highest = highestValue.ToDecimal();
			var lowest = lowestValue.ToDecimal();

			var diff = highest - lowest;

			if (diff != 0)
			{
				var pgo = (candle.ClosePrice - sma) / diff * 100;
				return pgo;
			}
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_sma.Reset();
		_highest.Reset();
		_lowest.Reset();

		_sma.Length = _highest.Length = _lowest.Length = Length;

		base.Reset();
	}
}