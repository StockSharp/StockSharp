namespace StockSharp.Algo.Indicators;

/// <summary>
/// Force Index indicator, also known as Elder's Force Index (EFI).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FIKey,
	Description = LocalizedStrings.ForceIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/force_index.html")]
public class ForceIndex : ExponentialMovingAverage
{
	private decimal _prevClosePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ForceIndex"/>.
	/// </summary>
	public ForceIndex()
		: base()
	{
		Length = 13;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClosePrice = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClosePrice == 0)
		{
			if (input.IsFinal)
				_prevClosePrice = candle.ClosePrice;

			return null;
		}

		var force = (candle.ClosePrice - _prevClosePrice) * candle.TotalVolume;

		var emaValue = base.OnProcessDecimal(input.SetValue(this, force));

		if (input.IsFinal)
			_prevClosePrice = candle.ClosePrice;

		return emaValue;
	}
}
