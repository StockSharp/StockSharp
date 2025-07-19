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
[IndicatorOut(typeof(PivotPointsValue))]
public class PivotPoints : BaseComplexIndicator<PivotPointsValue>
{
	/// <summary>
	/// Pivot point.
	/// </summary>
	[Browsable(false)]
	public PivotPointPart PivotPoint { get; } = new() { Name = "PivotPoint" };

	/// <summary>
	/// Resistance level R1.
	/// </summary>
	[Browsable(false)]
	public PivotPointPart R1 { get; } = new() { Name = "R1" };

	/// <summary>
	/// Resistance level R2.
	/// </summary>
	[Browsable(false)]
	public PivotPointPart R2 { get; } = new() { Name = "R2" };

	/// <summary>
	/// Support level S1.
	/// </summary>
	[Browsable(false)]
	public PivotPointPart S1 { get; } = new() { Name = "S1" };

	/// <summary>
	/// Support level S2.
	/// </summary>
	[Browsable(false)]
	public PivotPointPart S2 { get; } = new() { Name = "S2" };

	/// <summary>
	/// Initializes a new instance of the <see cref="PivotPoints"/>.
	/// </summary>
	public PivotPoints()
	{
		AddInner(PivotPoint);
		AddInner(R1);
		AddInner(R2);
		AddInner(S1);
		AddInner(S2);
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		var result = new PivotPointsValue(this, input.Time);

		var pivotPoint = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;

		result.Add(PivotPoint, PivotPoint.Process(pivotPoint, input.Time, input.IsFinal));
		result.Add(R1, R1.Process(2 * pivotPoint - candle.LowPrice, input.Time, input.IsFinal));
		result.Add(R2, R2.Process(pivotPoint + cl, input.Time, input.IsFinal));
		result.Add(S1, S1.Process(2 * pivotPoint - candle.HighPrice, input.Time, input.IsFinal));
		result.Add(S2, S2.Process(pivotPoint - cl, input.Time, input.IsFinal));

		return result;
	}

	/// <inheritdoc />
	protected override PivotPointsValue CreateValue(DateTimeOffset time)
		=> new(this, time);
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

/// <summary>
/// <see cref="PivotPoints"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PivotPointsValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="PivotPoints"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class PivotPointsValue(PivotPoints indicator, DateTimeOffset time) : ComplexIndicatorValue<PivotPoints>(indicator, time)
{
	/// <summary>
	/// Gets the Pivot Point value.
	/// </summary>
	public IIndicatorValue PivotPointValue => this[TypedIndicator.PivotPoint];

	/// <summary>
	/// Gets the Pivot Point value.
	/// </summary>
	[Browsable(false)]
	public decimal? PivotPoint => PivotPointValue.ToNullableDecimal();

	/// <summary>
	/// Gets the R1 value.
	/// </summary>
	public IIndicatorValue R1Value => this[TypedIndicator.R1];

	/// <summary>
	/// Gets the R1 value.
	/// </summary>
	[Browsable(false)]
	public decimal? R1 => R1Value.ToNullableDecimal();

	/// <summary>
	/// Gets the R2 value.
	/// </summary>
	public IIndicatorValue R2Value => this[TypedIndicator.R2];

	/// <summary>
	/// Gets the R2 value.
	/// </summary>
	[Browsable(false)]
	public decimal? R2 => R2Value.ToNullableDecimal();

	/// <summary>
	/// Gets the S1 value.
	/// </summary>
	public IIndicatorValue S1Value => this[TypedIndicator.S1];

	/// <summary>
	/// Gets the S1 value.
	/// </summary>
	[Browsable(false)]
	public decimal? S1 => S1Value.ToNullableDecimal();

	/// <summary>
	/// Gets the S2 value.
	/// </summary>
	public IIndicatorValue S2Value => this[TypedIndicator.S2];

	/// <summary>
	/// Gets the S2 value.
	/// </summary>
	[Browsable(false)]
	public decimal? S2 => S2Value.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"PivotPoint={PivotPoint}, R1={R1}, R2={R2}, S1={S1}, S2={S2}";
}
