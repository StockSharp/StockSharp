namespace StockSharp.Algo.Indicators;

/// <summary>
/// Standard deviation.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/standard_deviation.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StdDevKey,
	Description = LocalizedStrings.StandardDeviationKey)]
[Doc("topics/api/indicators/list_of_indicators/standard_deviation.html")]
public class StandardDeviation : LengthIndicator<decimal>
{
	private readonly SimpleMovingAverage _sma;

	/// <summary>
	/// Initializes a new instance of the <see cref="StandardDeviation"/>.
	/// </summary>
	public StandardDeviation()
	{
		_sma = new SimpleMovingAverage();
		Length = 10;
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
		var newValue = input.ToDecimal();
		var smaValue = _sma.Process(input).ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		var buff = input.IsFinal ? Buffer : (IList<decimal>)[.. Buffer.Skip(1), newValue];

		//считаем значение отклонения в последней точке
		var std = buff.Select(t1 => t1 - smaValue).Select(t => t * t).Sum();

		return (decimal)Math.Sqrt((double)(std / Length));
	}
}