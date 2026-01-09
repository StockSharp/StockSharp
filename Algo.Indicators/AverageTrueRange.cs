namespace StockSharp.Algo.Indicators;

/// <summary>
/// The average true range <see cref="TrueRange"/>.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/atr.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ATRKey,
	Description = LocalizedStrings.AverageTrueRangeKey)]
[Doc("topics/api/indicators/list_of_indicators/atr.html")]
public class AverageTrueRange : WilderMovingAverage
{
	private readonly TrueRange _trueRange = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageTrueRange"/>.
	/// </summary>
	public AverageTrueRange()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_trueRange.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
		=> base.OnProcess(_trueRange.Process(input));
}
