namespace StockSharp.Algo.Indicators;

/// <summary>
/// Money Flow Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/money_flow_index.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MFIKey,
	Description = LocalizedStrings.MoneyFlowIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/money_flow_index.html")]
public class MoneyFlowIndex : LengthIndicator<decimal>
{
	private decimal _previousPrice;
	private readonly Sum _positiveFlow = new();
	private readonly Sum _negativeFlow = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFlowIndex"/>.
	/// </summary>
	public MoneyFlowIndex()
	{
	    Length = 14;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFlowIndex"/>.
	/// </summary>
	/// <param name="length">Period length.</param>
	public MoneyFlowIndex(int length)
	{
	    Length = length;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_positiveFlow.Length = _negativeFlow.Length = Length;
		_previousPrice = 0;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _positiveFlow.IsFormed && _negativeFlow.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3.0m;
		var moneyFlow = typicalPrice * candle.TotalVolume;
		
		var positiveFlow = _positiveFlow.Process(input, typicalPrice > _previousPrice ? moneyFlow : 0.0m).ToDecimal();
		var negativeFlow = _negativeFlow.Process(input, typicalPrice < _previousPrice ? moneyFlow : 0.0m).ToDecimal();

		_previousPrice = typicalPrice;
		
		if (negativeFlow == 0)
			return new DecimalIndicatorValue(this, 100m, input.Time);
		
		if (positiveFlow / negativeFlow == 1)
			return new DecimalIndicatorValue(this, 0m, input.Time);

		return negativeFlow != 0 
			? new DecimalIndicatorValue(this, 100m - 100m / (1m + positiveFlow / negativeFlow), input.Time)
			: new DecimalIndicatorValue(this, input.Time);
	}
}
