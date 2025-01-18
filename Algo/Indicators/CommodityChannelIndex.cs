namespace StockSharp.Algo.Indicators;

/// <summary>
/// Commodity Channel Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/cci.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CCIKey,
	Description = LocalizedStrings.CommodityChannelIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/cci.html")]
public class CommodityChannelIndex : LengthIndicator<decimal>
{
	private readonly MeanDeviation _mean = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="CommodityChannelIndex"/>.
	/// </summary>
	public CommodityChannelIndex()
	{
		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override void Reset()
	{
		_mean.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _mean.IsFormed;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var aveP = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var meanValue = _mean.Process(new DecimalIndicatorValue(this, aveP, input.Time) { IsFinal = input.IsFinal });

		if (IsFormed && meanValue.ToDecimal() != 0)
			return (aveP - _mean.Sma.GetCurrentValue()) / (0.015m * meanValue.ToDecimal());

		return null;
	}
}