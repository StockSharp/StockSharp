namespace StockSharp.Algo.Indicators;

/// <summary>
/// Average deviation.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/mean_deviation.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MeanDevKey,
	Description = LocalizedStrings.AverageDeviationKey)]
[Doc("topics/api/indicators/list_of_indicators/mean_deviation.html")]
public class MeanDeviation : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MeanDeviation"/>.
	/// </summary>
	public MeanDeviation()
	{
		Sma = new SimpleMovingAverage();
		Length = 5;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// Moving Average.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Sma.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		Sma.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var val = input.ToDecimal();

		if (input.IsFinal)
			Buffer.PushBack(val);

		var smaValue = Sma.Process(input).ToDecimal();

		if (Buffer.Count > Length)
			Buffer.PopFront();

		// считаем значение отклонения
		var md = input.IsFinal
			? Buffer.Sum(t => Math.Abs(t - smaValue))
			: Buffer.Skip(IsFormed ? 1 : 0).Sum(t => Math.Abs(t - smaValue)) + Math.Abs(val - smaValue);

		return md / Length;
	}
}