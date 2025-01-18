namespace StockSharp.Algo.Indicators;

/// <summary>
/// QStick.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/qstick.html
/// </remarks>
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/qstick.html")]
public class QStick : LengthIndicator<IIndicatorValue>
{
	private readonly SimpleMovingAverage _sma;

	/// <summary>
	/// Initializes a new instance of the <see cref="QStick"/>.
	/// </summary>
	public QStick()
	{
		_sma = new SimpleMovingAverage();
		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _sma.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_sma.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var val = _sma.Process(input, candle.OpenPrice - candle.ClosePrice);
		return val.ToDecimal();
	}
}
