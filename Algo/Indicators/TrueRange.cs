namespace StockSharp.Algo.Indicators;

/// <summary>
/// True range.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/true_range.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TRKey,
	Description = LocalizedStrings.TrueRangeKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/true_range.html")]
public class TrueRange : BaseIndicator
{
	private ICandleMessage _prevCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrueRange"/>.
	/// </summary>
	public TrueRange()
	{
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => 2;

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_prevCandle = null;
	}

	/// <summary>
	/// To get price components to select the maximal value.
	/// </summary>
	/// <param name="currentCandle">The current candle.</param>
	/// <param name="prevCandle">The previous candle.</param>
	/// <returns>Price components.</returns>
	protected virtual decimal[] GetPriceMovements(ICandleMessage currentCandle, ICandleMessage prevCandle)
	{
		return
		[
			Math.Abs(currentCandle.HighPrice - currentCandle.LowPrice),
			Math.Abs(prevCandle.ClosePrice - currentCandle.HighPrice),
			Math.Abs(prevCandle.ClosePrice - currentCandle.LowPrice)
		];
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevCandle != null)
		{
			if (input.IsFinal)
				IsFormed = true;

			var priceMovements = GetPriceMovements(candle, _prevCandle);

			if (input.IsFinal)
				_prevCandle = candle;

			return new DecimalIndicatorValue(this, priceMovements.Max(), input.Time);
		}

		if (input.IsFinal)
			_prevCandle = candle;

		return new DecimalIndicatorValue(this, candle.HighPrice - candle.LowPrice, input.Time);
	}
}
