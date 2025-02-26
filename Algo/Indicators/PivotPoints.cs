namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Pivot Points indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PPKey,
	Description = LocalizedStrings.PivotPointsKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/pivot_points.html")]
public class PivotPoints : BaseComplexIndicator
{
	private readonly PivotPointPart _pivotPoint = new() { Name = "PivotPoint" };
	private readonly PivotPointPart _r1 = new() { Name = "R1" };
	private readonly PivotPointPart _r2 = new() { Name = "R2" };
	private readonly PivotPointPart _s1 = new() { Name = "S1" };
	private readonly PivotPointPart _s2 = new() { Name = "S2" };

	/// <summary>
	/// Initializes a new instance of the <see cref="PivotPoints"/>.
	/// </summary>
	public PivotPoints()
	{
		AddInner(_pivotPoint);
		AddInner(_r1);
		AddInner(_r2);
		AddInner(_s1);
		AddInner(_s2);
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		var result = new ComplexIndicatorValue(this, input.Time);

		var pivotPoint = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;

		result.Add(_pivotPoint, _pivotPoint.Process(pivotPoint, input.Time));
		result.Add(_r1, _r1.Process(2 * pivotPoint - candle.LowPrice, input.Time));
		result.Add(_r2, _r2.Process(pivotPoint + cl, input.Time));
		result.Add(_s1, _s1.Process(2 * pivotPoint - candle.HighPrice, input.Time));
		result.Add(_s2, _s2.Process(pivotPoint - cl, input.Time));

		return result;
	}
}

/// <summary>
/// Represents a part of the Pivot Points indicator.
/// </summary>
[IndicatorHidden]
public class PivotPointPart : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;
		
		return input;
	}
}