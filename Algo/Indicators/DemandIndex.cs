namespace StockSharp.Algo.Indicators;

/// <summary>
/// Demand Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DIKey,
	Description = LocalizedStrings.DemandIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/demand_index.html")]
public class DemandIndex : SimpleMovingAverage
{
	private decimal _prevClose;
	private decimal _prevVolume;
	private decimal _prevValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="DemandIndex"/>.
	/// </summary>
	public DemandIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose == 0 || _prevVolume == 0)
		{
			if (input.IsFinal)
			{
				_prevClose = candle.ClosePrice;
				_prevVolume = candle.TotalVolume;
			}

			return null;
		}

		var deltaP = candle.ClosePrice - _prevClose;
		var deltaV = candle.TotalVolume - _prevVolume;

		if (deltaP == 0 || deltaV == 0)
			return _prevValue;

		var logDeltaP = deltaP.Abs().Log();
		var logDeltaV = deltaV.Abs().Log();

		var a = logDeltaP * logDeltaV;
		var b = logDeltaP - logDeltaV;

		var demandIndex = 0m;

		if (b != 0)
			demandIndex = a / b;

		demandIndex *= Math.Sign(deltaP);

		var result = base.OnProcessDecimal(new DecimalIndicatorValue(this, demandIndex, input.Time) { IsFinal = input.IsFinal });

		if (input.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			_prevVolume = candle.TotalVolume;
			_prevValue = result.Value;
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		_prevVolume = 0;
		_prevValue = 0;

		base.Reset();
	}
}