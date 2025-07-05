namespace StockSharp.Algo.Indicators;

/// <summary>
/// Elder's Force Index.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ElderForceIndexKey,
	Description = LocalizedStrings.ElderForceIndexDescriptionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/elder_force_index.html")]
public class ElderForceIndex : ExponentialMovingAverage
{
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="ElderForceIndex"/>.
	/// </summary>
	public ElderForceIndex()
	{
		Length = 13;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose == 0)
		{
			if (input.IsFinal)
				_prevClose = candle.ClosePrice;

			return new DecimalIndicatorValue(this, input.Time);
		}

		// Force Index = Volume * (Close - Previous Close)
		var forceIndex = candle.TotalVolume * (candle.ClosePrice - _prevClose);

		if (input.IsFinal)
			_prevClose = candle.ClosePrice;

		// Apply EMA smoothing
		var emaValue = base.OnProcess(input.SetValue(this, forceIndex));

		return new DecimalIndicatorValue(this, emaValue.ToDecimal(), input.Time);
	}
}